using System;

namespace GridInfect.Core
{
    // The tutorial: ten hand-authored boards, one mechanic and one sentence
    // each, played in order on the ordinary board screen with the solution
    // marked. They are not levels. Every board is small, and every one has
    // exactly one solution (TutorialTests pins that with the solution
    // counter), so "does not advance until the marks are hit" is the plain
    // win check and nothing more.
    //
    // The order is the order the worlds meet things in — dragging, gaps, a
    // second bug and the undo, walls, diagonals, the blot, avoid cells —
    // then the two Legacy elements no generated world uses yet (repel,
    // trap), and last a relay chain that is as much a demonstration as a
    // lesson. Never generated, never baked: authored here, RulesV2.
    public static class TutorialLevels
    {
        public sealed class Step
        {
            public readonly LevelDef Def;
            public readonly (int piece, int cell)[] Solution;   // the marks, in a winning order

            // The one sentence over the board is not here. It is prose, and
            // Core holds keys and never prose (ARCHITECTURE.md §8): the adapter
            // reads tut.<step>.line from the string table. The sentence each
            // board teaches is kept beside it below as a comment, so this
            // file still reads as the lesson plan it is.
            internal Step(LevelDef def, (int piece, int cell)[] solution)
            {
                Def = def;
                Solution = solution;
            }
        }

        static Step[] _all;

        public static int Count => All.Length;

        public static Step Get(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return All[index];
        }

        public static Step[] All
        {
            get
            {
                if (_all == null) _all = Build();
                return _all;
            }
        }

        // Rows top to bottom, six characters each: '.' void, '#' active,
        // 'W' wall, 'R' repel switch, 'T' reset trap, 'X' avoid. A relay is
        // an active cell with arms listed separately as (row, col, arms).
        static Step[] Build() => new[]
        {
            // 1. A plus. The only cell on both its lines is the middle.
            Make(   // Drag the bug onto the marked cell.
                new[]
                {
                    "......",
                    "......",
                    "..#...",
                    "..#...",
                    "..#...",
                    "######",
                    "..#...",
                    "..#...",
                    "..#...",
                    "......",
                    "......",
                },
                "LRUD", At(0, 5, 2)),

            // 2. The same plus with its arms broken: a ray does not stop at
            // a gap, which is the one rule a still board cannot show.
            Make(   // Rays cross gaps.
                new[]
                {
                    "......",
                    "......",
                    "..#...",
                    "..#...",
                    "......",
                    "#.#.##",
                    "..#...",
                    "..#...",
                    "..#...",
                    "......",
                    "......",
                },
                "LRUD", At(0, 5, 2)),

            // 3. Two single-arm bugs at the two ends of an L: each must sit
            // where its one ray covers the whole leg. A player who drops one
            // wrong learns the undo here, which is why the sentence says it.
            Make(   // Two bugs. Tap a placed bug to pick it up.
                new[]
                {
                    "......",
                    "......",
                    "......",
                    "######",
                    ".....#",
                    ".....#",
                    ".....#",
                    ".....#",
                    ".....#",
                    "......",
                    "......",
                },
                "R,U", At(0, 3, 0), At(1, 8, 5)),

            // 4. A wall in the row. The RD bug's ray stops at it and the two
            // cells beyond stay dark until the L bug takes them from the
            // far end.
            Make(   // A wall stops a ray.
                new[]
                {
                    "......",
                    "......",
                    "......",
                    "......",
                    "......",
                    "###W##",
                    "#.....",
                    "#.....",
                    "#.....",
                    "......",
                    "......",
                },
                "RD,L", At(0, 5, 0), At(1, 5, 5)),

            // 5. An X. The four-diagonal bug only covers it from the centre.
            Make(   // Bugs can also be diagonal.
                new[]
                {
                    "......",
                    "......",
                    ".....#",
                    "#...#.",
                    ".#.#..",
                    "..#...",
                    ".#.#..",
                    "#...#.",
                    ".....#",
                    "......",
                    "......",
                },
                "ul+ur+dl+dr", At(0, 5, 2)),

            // 6. A 3x3 square and the blot.
            Make(   // A blot takes the eight cells around it.
                new[]
                {
                    "......",
                    "......",
                    "......",
                    "......",
                    ".###..",
                    ".###..",
                    ".###..",
                    "......",
                    "......",
                    "......",
                    "......",
                },
                "A", At(0, 5, 2)),

            // 7. Two rows, an avoid cell at one end of each. The L bug's row
            // has its avoid on the right, the R bug's on the left: either bug
            // in the other row would touch it, and the drop bounces.
            Make(   // A ray may never touch an avoid cell.
                new[]
                {
                    "......",
                    "......",
                    "......",
                    "..###X",
                    "......",
                    "X###..",
                    "......",
                    "......",
                    "......",
                    "......",
                    "......",
                },
                "L,R", At(0, 3, 4), At(1, 5, 1)),

            // 8. The R bug's ray runs three cells into the repel and is swept
            // back to the bug; the blot, sat in the middle of the swept
            // row, lights the block again. (Placed last, the R bug wins
            // outright: the win check runs before the repel, RULES §4.1.)
            // Two bugs of one axis each would swap roles, so the second is
            // a blot, and the block is three rows so it has one centre.
            Make(   // A repel clears the ray that hit it.
                new[]
                {
                    "......",
                    "......",
                    "......",
                    "......",
                    ".###..",
                    "####R.",
                    ".###..",
                    "......",
                    "......",
                    "......",
                    "......",
                },
                "R,A", At(0, 5, 0), At(1, 5, 2)),

            // 9. Two columns with a trap at one end of each, mirrored: the
            // U bug's column has its trap below, the D bug's above. A bug
            // pointed at a trap trips it and the board comes back empty.
            Make(   // A trap resets the whole board.
                new[]
                {
                    "......",
                    "......",
                    ".#..T.",
                    ".#..#.",
                    ".#..#.",
                    ".T..#.",
                    "......",
                    "......",
                    "......",
                    "......",
                    "......",
                },
                "U,D", At(0, 4, 1), At(1, 3, 4)),

            // 10. A spiral of relays. One R bug at the tail lights the top
            // row; the relay at its end fires down, the next fires left, and
            // so on inward, seven turns to the middle. The tail cell is in a
            // column no ray runs up, so the bug has one place to go.
            Make(   // A relay fires its own rays when lit.
                new[]
                {
                    "......",
                    "######",
                    ".....#",
                    ".###.#",
                    ".#.#.#",
                    ".###.#",
                    ".###.#",
                    ".###.#",
                    ".#...#",
                    ".#####",
                    "......",
                },
                "R", new[] { At(0, 1, 0) },
                Relay(1, 5, Dir.D), Relay(9, 5, Dir.L), Relay(9, 1, Dir.U), Relay(3, 1, Dir.R),
                Relay(3, 3, Dir.D), Relay(7, 3, Dir.L), Relay(7, 2, Dir.U)),
        };

        static (int piece, int cell) At(int piece, int i, int j) => (piece, Grid.Loc(i, j));

        static (int cell, byte arms) Relay(int i, int j, params Dir[] dirs)
        {
            int arms = 0;
            foreach (Dir d in dirs) arms |= 1 << (int)d;
            return (Grid.Loc(i, j), (byte)arms);
        }

        static Step Make(string[] rows, string pieces, params (int piece, int cell)[] solution) =>
            Make(rows, pieces, solution, Array.Empty<(int, byte)>());

        static Step Make(string[] rows, string pieces, (int piece, int cell)[] solution,
            params (int cell, byte arms)[] relays)
        {
            if (rows.Length != Grid.Height) throw new InvalidOperationException("tutorial board: row count");
            var board = new byte[Grid.Cells];
            for (int i = 0; i < Grid.Height; i++)
            {
                if (rows[i].Length != Grid.Width) throw new InvalidOperationException($"tutorial board: row {i} width");
                for (int j = 0; j < Grid.Width; j++)
                {
                    board[Grid.Loc(i, j)] = rows[i][j] switch
                    {
                        '.' => Cell.Void,
                        '#' => Cell.Active,
                        'W' => Cell.Wall,
                        'R' => Cell.RepelSwitch,
                        'T' => Cell.ResetTrap,
                        'X' => Cell.Forbidden,
                        _ => throw new InvalidOperationException($"tutorial board: cell '{rows[i][j]}'"),
                    };
                }
            }
            byte[] cellData = null;
            if (relays.Length > 0)
            {
                cellData = new byte[Grid.Cells];
                foreach (var (cell, arms) in relays) cellData[cell] = arms;
            }
            string[] names = pieces.Split(',');
            var specs = new PieceSpec[names.Length];
            for (int k = 0; k < names.Length; k++) specs[k] = PieceSpec.Parse(names[k]);
            return new Step(new LevelDef(board, specs, cellData), solution);
        }
    }
}
