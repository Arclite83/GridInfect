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
            Families = families ?? Solving.Families.Cardinal;
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

        public static bool IsBlocker(byte v) => v == Cell.Wall || v == Cell.RepelSwitch || v == Cell.ResetTrap;

        // Cells infected by placing `tile` at `loc` on the static board.
        public CellMask Coverage(Tile tile, int loc)
        {
            var m = CellMask.Bit(loc);
            for (int f = 0; f < Families.Length; f++)
            {
                int id = LineOf[f][loc];
                if (id < 0) continue;
                Line line = Lines[f][id];
                int at = IndexIn[f][loc];
                if (TileArms.Has(tile, Families[f].Pos))
                {
                    for (int n = at + 1; n < line.Cells.Length; n++) m |= CellMask.Bit(line.Cells[n]);
                }
                if (TileArms.Has(tile, Families[f].Neg))
                {
                    for (int n = at - 1; n >= 0; n--) m |= CellMask.Bit(line.Cells[n]);
                }
            }
            return m & ActiveMask;
        }

        // The arm from `loc` toward `dir` ends on this cell value (a blocker) or
        // Void when it runs off the board. Used to spot trap trips and switch hits.
        public byte ArmEndsOn(int loc, Dir dir)
        {
            int i = loc / Grid.Width + TileArms.Di(dir), j = loc % Grid.Width + TileArms.Dj(dir);
            while (Grid.InBounds(i, j))
            {
                byte v = Def.BoardAt(Grid.Loc(i, j));
                if (IsBlocker(v)) return v;
                i += TileArms.Di(dir);
                j += TileArms.Dj(dir);
            }
            return Cell.Void;
        }

        public bool TripsTrap(Tile tile, int loc)
        {
            for (int d = 0; d < 4; d++)
            {
                var dir = (Dir)d;
                if (TileArms.Has(tile, dir) && ArmEndsOn(loc, dir) == Cell.ResetTrap) return true;
            }
            return false;
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
