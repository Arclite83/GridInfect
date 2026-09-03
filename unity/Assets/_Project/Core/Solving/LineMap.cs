using System;
using System.Collections.Generic;

namespace GridInfect.Core.Solving
{
    // A direction family is a pair of opposite arms that walk the same axis.
    // The solver reasons about lines (maximal runs of non-blocker cells along
    // a family's axis, bounded by walls/switches/traps or the board edge), so
    // a new axis is a new table entry here, not a new code path (NEXT_PASS §1).
    public readonly struct Family
    {
        public readonly string Name;
        public readonly Dir Neg;   // the arm that walks toward the line's start
        public readonly Dir Pos;   // the arm that walks toward the line's end

        public Family(string name, Dir neg, Dir pos)
        {
            Name = name;
            Neg = neg;
            Pos = pos;
        }
    }

    public static class Families
    {
        public static readonly Family[] Cardinal =
        {
            new Family("row", Dir.L, Dir.R),
            new Family("column", Dir.U, Dir.D),
        };

        public static readonly Family[] All =
        {
            new Family("row", Dir.L, Dir.R),
            new Family("column", Dir.U, Dir.D),
            new Family("diagonal", Dir.UL, Dir.DR),
            new Family("antidiagonal", Dir.UR, Dir.DL),
        };

        // The families a level needs: the diagonals only when a piece or a
        // relay has a diagonal arm.
        public static Family[] For(LevelDef def)
        {
            bool diagonal = false;
            foreach (PieceSpec spec in def.Specs) diagonal |= spec.HasDiagonal;
            for (int loc = 0; loc < Grid.Cells && !diagonal; loc++) diagonal |= (def.CellDataAt(loc) & 0xF0) != 0;
            return diagonal ? All : Cardinal;
        }
    }

    // One line: the cells along a family's axis between two blockers (or
    // edges), in Pos order. Voids stay in the line (arms jump gaps) but are
    // never targets; `Active` holds the cells that need covering.
    public sealed class Line
    {
        public readonly int Family;
        public readonly int Id;
        public readonly int[] Cells;    // every non-blocker cell, Pos order
        public readonly CellMask Active;   // the value-1 cells

        public Line(int family, int id, int[] cells, CellMask active)
        {
            Family = family;
            Id = id;
            Cells = cells;
            Active = active;
        }
    }

    // The board decomposed into lines, plus static coverage per (piece, cell).
    // Static coverage treats switches and traps as blockers and ignores
    // repels; the Python oracle (tools/level_metrics.py) does the same and
    // checks order feasibility on the final assignment only.
    public sealed class LineMap
    {
        public readonly LevelDef Def;
        public readonly Family[] Families;
        public readonly List<Line>[] Lines;       // per family
        public readonly int[][] LineOf;           // [family][loc] -> line id or -1
        public readonly int[][] IndexIn;          // [family][loc] -> index in Line.Cells
        public readonly CellMask ActiveMask;      // cells that must be covered
        public readonly bool HasDynamics;         // any switch or trap on the board
        public readonly bool HasSwitches;         // any repel switch on the board

        public LineMap(LevelDef def, Family[] families = null)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            Families = families ?? Solving.Families.For(def);
            Lines = new List<Line>[Families.Length];
            LineOf = new int[Families.Length][];
            IndexIn = new int[Families.Length][];

            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                byte v = def.BoardAt(loc);
                if (v == Cell.Active) ActiveMask |= CellMask.Bit(loc);
                if (v == Cell.RepelSwitch || v == Cell.ResetTrap) HasDynamics = true;
                if (v == Cell.RepelSwitch) HasSwitches = true;
            }

            for (int f = 0; f < Families.Length; f++)
            {
                Lines[f] = new List<Line>();
                LineOf[f] = new int[Grid.Cells];
                IndexIn[f] = new int[Grid.Cells];
                for (int loc = 0; loc < Grid.Cells; loc++) LineOf[f][loc] = -1;

                int di = TileArms.Di(Families[f].Pos), dj = TileArms.Dj(Families[f].Pos);
                var run = new List<int>(Grid.Height);
                for (int loc = 0; loc < Grid.Cells; loc++)
                {
                    if (LineOf[f][loc] >= 0 || IsBlocker(def.BoardAt(loc))) continue;
                    int i = loc / Grid.Width, j = loc % Grid.Width;
                    // Only start a line at its Neg end.
                    int pi = i - di, pj = j - dj;
                    if (Grid.InBounds(pi, pj) && !IsBlocker(def.BoardAt(Grid.Loc(pi, pj)))) continue;

                    run.Clear();
                    var active = CellMask.None;
                    while (Grid.InBounds(i, j) && !IsBlocker(def.BoardAt(Grid.Loc(i, j))))
                    {
                        int c = Grid.Loc(i, j);
                        LineOf[f][c] = Lines[f].Count;
                        IndexIn[f][c] = run.Count;
                        run.Add(c);
                        if (def.BoardAt(c) == Cell.Active) active |= CellMask.Bit(c);
                        i += di;
                        j += dj;
                    }
                    Lines[f].Add(new Line(f, Lines[f].Count, run.ToArray(), active));
                }
            }
        }

        public static bool IsBlocker(byte v) => v == Cell.Wall || v == Cell.RepelSwitch || v == Cell.ResetTrap || v == Cell.Forbidden;

        public CellMask Coverage(Tile tile, int loc) => Coverage(PieceSpec.FromTile(tile), loc);

        // Cells infected by placing `spec` at `loc` on the static board: the
        // RulesV2 spread (arms with reach, the area, relay chains) with
        // switches and traps as blockers and repels ignored — the static
        // model the oracle uses; order feasibility is checked at the end.
        public CellMask Coverage(PieceSpec spec, int loc) => Spread(spec, loc).Covered;

        public bool TripsTrap(Tile tile, int loc) => TripsTrap(PieceSpec.FromTile(tile), loc);

        public bool TripsTrap(PieceSpec spec, int loc) => Spread(spec, loc).Trips;

        // A spread that would touch a forbidden cell is not a legal placement.
        public bool IsIllegal(PieceSpec spec, int loc) => Spread(spec, loc).Forbidden;

        public readonly struct SpreadResult
        {
            public readonly CellMask Covered;
            public readonly bool Trips;
            public readonly bool Forbidden;
            public readonly bool Switches;

            public SpreadResult(CellMask covered, bool trips, bool forbidden, bool switches)
            {
                Covered = covered;
                Trips = trips;
                Forbidden = forbidden;
                Switches = switches;
            }
        }

        public SpreadResult Spread(PieceSpec spec, int loc)
        {
            var covered = CellMask.None;
            bool trips = false, forbidden = false, switches = false;
            int i0 = loc / Grid.Width, j0 = loc % Grid.Width;
            var pending = new System.Collections.Generic.Stack<(int cell, byte arms, uint reach)>();

            Infect(loc, ref covered, pending);
            if (spec.Area)
            {
                for (int di = -1; di <= 1; di++)
                {
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        if (di == 0 && dj == 0) continue;
                        int ai = i0 + di, aj = j0 + dj;
                        if (!Grid.InBounds(ai, aj)) continue;
                        byte v = Def.BoardAt(Grid.Loc(ai, aj));
                        if (v == Cell.Forbidden) forbidden = true;
                        else if (v == Cell.Active) Infect(Grid.Loc(ai, aj), ref covered, pending);
                    }
                }
            }
            if (spec.Arms != 0) pending.Push((loc, spec.Arms, spec.Reach));

            while (pending.Count > 0)
            {
                var (cell, arms, reach) = pending.Pop();
                int ci = cell / Grid.Width, cj = cell % Grid.Width;
                for (int d = 0; d < 8; d++)
                {
                    if ((arms & (1 << d)) == 0) continue;
                    var dir = (Dir)d;
                    int limit = (int)(reach >> (4 * d)) & 0xF;
                    for (int offset = 1; offset <= Grid.SpreadRange; offset++)
                    {
                        if (limit != 0 && offset > limit) break;
                        int i = ci + TileArms.Di(dir) * offset, j = cj + TileArms.Dj(dir) * offset;
                        if (!Grid.InBounds(i, j)) continue;
                        byte v = Def.BoardAt(Grid.Loc(i, j));
                        if (v == Cell.Wall) break;
                        if (v == Cell.RepelSwitch) { switches = true; break; }
                        if (v == Cell.ResetTrap) { trips = true; break; }
                        if (v == Cell.Forbidden) { forbidden = true; break; }
                        if (v == Cell.Active) Infect(Grid.Loc(i, j), ref covered, pending);
                    }
                }
            }
            return new SpreadResult(covered & ActiveMask, trips, forbidden, switches);
        }

        void Infect(int loc, ref CellMask covered, System.Collections.Generic.Stack<(int cell, byte arms, uint reach)> pending)
        {
            if (covered.Has(loc)) return;
            covered |= CellMask.Bit(loc);
            byte relay = Def.CellDataAt(loc);
            if (relay != 0) pending.Push((loc, relay, 0u));
        }

        // Every cell in the two lines through `loc` (the cells a line rule
        // reasons about), for deduction evidence.
        public int[] LinesThrough(int loc)
        {
            var cells = new List<int>();
            for (int f = 0; f < Families.Length; f++)
            {
                int id = LineOf[f][loc];
                if (id >= 0) cells.AddRange(Lines[f][id].Cells);
            }
            return cells.ToArray();
        }
    }
}
