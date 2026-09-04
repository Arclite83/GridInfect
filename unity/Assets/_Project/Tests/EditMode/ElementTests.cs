using GridInfect.Core.Generation;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Stages 8–12: one rules check and one generator check per element,
    // through the real V2 rules and the real pipeline.
    [TestFixture]
    public class ElementTests
    {
        static LevelDef V2(string rows, string specs, byte[] cellData = null)
        {
            var board = new byte[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells && loc < rows.Length; loc++)
            {
                board[loc] = rows[loc] == '.' ? Cell.Void : (byte)(rows[loc] - '0');
            }
            string[] names = specs.Split(',');
            var parsed = new PieceSpec[names.Length];
            for (int k = 0; k < names.Length; k++) parsed[k] = PieceSpec.Parse(names[k]);
            return new LevelDef(board, parsed, cellData);
        }

        static void AssertAccepted(GenSpec spec, int count, string what)
        {
            int accepted = 0, withElement = 0;
            for (ulong seed = 1; accepted < count && seed < 3000; seed++)
            {
                var level = GeneratorV2.Generate(spec, seed);
                if (level == null) continue;
                accepted++;
                var placed = Locked.Placed(level.Def, level.Locks);
                Assert.That(SolutionCounter.Count(level.Def, placed), Is.EqualTo(1), $"{what} seed {seed}: unique");
                var solve = Deducer.Solve(level.Def, placed);
                Assert.That(solve.Solved && solve.Guesses == 0, Is.True, $"{what} seed {seed}: deducible");
                Assert.That(SolutionCounter.Wins(level.Def, level.Solution), Is.True, $"{what} seed {seed}: stored solution wins");
                if (UsesElement(level.Def, spec.Elements)) withElement++;
            }
            Assert.That(accepted, Is.EqualTo(count), $"{what}: not enough accepted boards");
            Assert.That(withElement, Is.GreaterThan(0), $"{what}: no accepted board uses the element");
        }

        static bool UsesElement(LevelDef def, Element elements)
        {
            bool any = false;
            foreach (PieceSpec spec in def.Specs)
            {
                if ((elements & Element.ShortArms) != 0 && spec.HasShortArm) any = true;
                if ((elements & Element.Area) != 0 && spec.Area) any = true;
                if ((elements & Element.Diagonals) != 0 && spec.HasDiagonal) any = true;
            }
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if ((elements & Element.Forbidden) != 0 && def.BoardAt(loc) == Cell.Forbidden) any = true;
                if ((elements & Element.Relays) != 0 && def.CellDataAt(loc) != 0) any = true;
            }
            return any;
        }

        // ---- stage 8: short arms ----

        [Test]
        public void ShortArmStopsAfterItsReachAndCountsVoidsAsRings()
        {
            // Column 2: piece at row 5 with U2 and D1; a void at row 4 is one ring.
            var def = V2("..1..." + "..1..." + "..1..." + "..1..." + "......" + "..1..." +
                         "..1..." + "..1..." + "......" + "......" + "......", "U2+D1");
            var s = new LevelSession(def);
            Assert.That(s.Rules.CanPlace(s, 0, 5, 2), Is.True);
            s.Rules.SetPiece(s, 0, 5, 2);
            s.Rules.Resolve(s);
            Assert.That(s.Board[Grid.Loc(3, 2)], Is.EqualTo(Cell.Infected), "ring 2 up, past the void");
            Assert.That(s.Board[Grid.Loc(2, 2)], Is.EqualTo(Cell.Active), "ring 3 up is out of reach");
            Assert.That(s.Board[Grid.Loc(6, 2)], Is.EqualTo(Cell.Infected), "ring 1 down");
            Assert.That(s.Board[Grid.Loc(7, 2)], Is.EqualTo(Cell.Active), "ring 2 down is out of reach");
            Assert.That(new LineMap(def).Coverage(def.Specs[0], Grid.Loc(5, 2)).Count, Is.EqualTo(3), "solver agrees");
        }

        // ---- stage 9: the area piece ----

        [Test]
        public void BlotInfectsItsNeighbourhoodAndBlockersInsideAreInert()
        {
            // Wall above, switch left, trap right of the blot's cell; all inert.
            var def = V2("......" + ".121.." + ".315.." + ".111.." + "......" + "......" +
                         "......" + "......" + "......" + "......" + "......", "A");
            var s = new LevelSession(def);
            Assert.That(s.Rules.CanPlace(s, 0, 2, 2), Is.True);
            s.Rules.SetPiece(s, 0, 2, 2);
            Assert.That(s.RepelQueue.Count, Is.EqualTo(0), "the switch inside the area queues nothing");
            Assert.That(s.ResetTripped, Is.False, "the trap inside the area trips nothing");
            s.Rules.Resolve(s);
            Assert.That(s.Solved, Is.True);
            foreach (int loc in new[] { Grid.Loc(1, 1), Grid.Loc(1, 3), Grid.Loc(3, 1), Grid.Loc(3, 2), Grid.Loc(3, 3) })
            {
                Assert.That(s.Board[loc], Is.EqualTo(Cell.Infected));
            }
            Assert.That(s.Board[Grid.Loc(1, 2)], Is.EqualTo(Cell.Wall));
            Assert.That(s.Board[Grid.Loc(2, 1)], Is.EqualTo(Cell.RepelSwitch));
            Assert.That(s.Board[Grid.Loc(2, 3)], Is.EqualTo(Cell.ResetTrap));
            Assert.That(new LineMap(def).Coverage(def.Specs[0], Grid.Loc(2, 2)).Count, Is.EqualTo(6), "solver agrees");
        }

        [Test]
        public void BlotBoardsGenerateUniqueAndDeducible()
        {
            var spec = new GenSpec { Elements = Element.Walls | Element.Area, MinPieces = 3, MaxPieces = 4, AreaChance = 10 };
            AssertAccepted(spec, 12, "area");
        }

        // ---- stage 10: forbidden cells ----

        [Test]
        public void PlacementWhoseSpreadWouldTouchAForbiddenCellIsIllegal()
        {
            // Row 3: forbidden at (3,0), actives (3,1)..(3,4); column 1 actives (1,1),(2,1).
            var def = V2("......" + ".1...." + ".1...." + "61111." + "......" + "......" +
                         "......" + "......" + "......" + "......" + "......", "LU,R");
            var s = new LevelSession(def);
            Assert.That(s.Rules.CanPlace(s, 0, 3, 1), Is.False, "LU at (3,1): the left arm would hit the forbidden cell");
            Assert.That(s.Rules.CanPlace(s, 1, 3, 1), Is.True, "R at (3,1) never looks left");
            Assert.That(s.Rules.CanPlace(s, 0, 3, 4), Is.False, "LU at (3,4): the left arm runs into it too");
            var map = new LineMap(def);
            Assert.That(map.IsIllegal(def.Specs[0], Grid.Loc(3, 1)), Is.True, "solver agrees");
            Assert.That(map.IsIllegal(def.Specs[1], Grid.Loc(3, 1)), Is.False);
            // The area piece is refused when a forbidden cell sits inside its neighbourhood.
            var blot = V2("......" + ".11..." + ".16..." + "......" + "......" + "......" +
                          "......" + "......" + "......" + "......" + "......", "A");
            var b = new LevelSession(blot);
            Assert.That(b.Rules.CanPlace(b, 0, 1, 1), Is.False);
            Assert.That(s.Board[Grid.Loc(3, 0)], Is.EqualTo(Cell.Forbidden), "the board is untouched by a refused placement");
        }

        [Test]
        public void ForbiddenBoardsGenerateUniqueAndDeducible()
        {
            var spec = new GenSpec { Elements = Element.Walls | Element.Forbidden, MinPieces = 3, MaxPieces = 4 };
            AssertAccepted(spec, 12, "forbidden");
        }

        // ---- stage 11: diagonal arms ----

        [Test]
        public void DiagonalArmWalksItsDiagonalAndRepelsBackAlongIt()
        {
            // Piece at (5,1) with dr: (6,2), (7,3) void, (8,4), then a switch at (9,5).
            var def = V2("......" + "......" + "......" + "......" + "......" + ".1...." +
                         "..1..." + "......" + "....1." + ".....3" + "......", "dr");
            var s = new LevelSession(def);
            s.Rules.SetPiece(s, 0, 5, 1);
            Assert.That(s.Board[Grid.Loc(6, 2)], Is.EqualTo(Cell.Infected));
            Assert.That(s.Board[Grid.Loc(8, 4)], Is.EqualTo(Cell.Infected), "the void at (7,3) is passed over");
            Assert.That(s.RepelQueue.Count, Is.EqualTo(1));
            Assert.That(s.RepelQueue[0].Direction, Is.EqualTo(Dir.UL), "the repel walks back along the diagonal");
            s.Rules.Resolve(s);
            Assert.That(s.Solved, Is.True, "a winning placement is not repelled");

            var blocked = V2("......" + "......" + "......" + "......" + "......" + ".1...." +
                             "..2..." + "...1.." + "......" + "......" + "......", "dr,ul");
            var b = new LevelSession(blocked);
            b.Rules.SetPiece(b, 0, 5, 1);
            b.Rules.Resolve(b);
            Assert.That(b.Board[Grid.Loc(7, 3)], Is.EqualTo(Cell.Active), "the wall at (6,2) stops the diagonal");
            var map = new LineMap(blocked);
            Assert.That(map.Families.Length, Is.EqualTo(4), "diagonal families join the line map");
            Assert.That(map.Coverage(blocked.Specs[1], Grid.Loc(7, 3)).Count, Is.EqualTo(1), "ul from (7,3) hits the wall too");
        }

        [Test]
        public void DiagonalSpecsFlipWithTheBoard()
        {
            var spec = PieceSpec.Parse("L+ul2");
            Assert.That(Canonical.Flip(spec, flipH: true, flipV: false).Encode(), Is.EqualTo("R+ur2"));
            Assert.That(Canonical.Flip(spec, flipH: false, flipV: true).Encode(), Is.EqualTo("L+dl2"));
            Assert.That(Canonical.Flip(spec, flipH: true, flipV: true).Encode(), Is.EqualTo("R+dr2"));
        }

        [Test]
        public void DiagonalBoardsGenerateUniqueAndDeducible()
        {
            var spec = new GenSpec { Elements = Element.Walls | Element.Diagonals, MinPieces = 3, MaxPieces = 4, DiagonalChance = 14 };
            AssertAccepted(spec, 12, "diagonals");
        }

        // ---- stage 12: relay cells ----

        [Test]
        public void RelayFiresOnceWhenLitAndChainsIntoTheNextRelay()
        {
            // R at (2,0) lights the relay at (2,3) (arms D); its down arm lights
            // the relay at (5,3) (arms R), which reaches (5,5); a trap at (5,4)? no — clean chain.
            var cellData = new byte[Grid.Cells];
            cellData[Grid.Loc(2, 3)] = (byte)(1 << (int)Dir.D);
            cellData[Grid.Loc(5, 3)] = (byte)(1 << (int)Dir.R);
            var def = V2("......" + "......" + "1111.." + "...1.." + "...1.." + "...111" +
                         "......" + "......" + "......" + "......" + "......", "R", cellData);
            var s = new LevelSession(def);
            s.Rules.SetPiece(s, 0, 2, 0);
            s.Rules.Resolve(s);
            Assert.That(s.Board[Grid.Loc(4, 3)], Is.EqualTo(Cell.Infected), "the first relay's down arm");
            Assert.That(s.Board[Grid.Loc(5, 5)], Is.EqualTo(Cell.Infected), "the second relay's right arm");
            Assert.That(s.Solved, Is.True);
            Assert.That(new LineMap(def).Coverage(def.Specs[0], Grid.Loc(2, 0)).Count, Is.EqualTo(9), "solver follows the chain");

            // Undo rebuilds the chain from the initial board.
            s.Rules.ClearPiece(s, 0);
            for (int loc = 0; loc < Grid.Cells; loc++) Assert.That(s.Board[loc], Is.EqualTo(def.BoardAt(loc)));

            // A relay arm into a trap trips at placement time, so a non-winning
            // placement that lights it resets.
            var trapData = new byte[Grid.Cells];
            trapData[Grid.Loc(2, 2)] = (byte)(1 << (int)Dir.D);
            var trapDef = V2("......" + "......" + "111..." + "..5..." + "......" + "1....." +
                             "......" + "......" + "......" + "......" + "......", "R,D", trapData);
            var t = new LevelSession(trapDef);
            t.Rules.SetPiece(t, 0, 2, 0);
            Assert.That(t.ResetTripped, Is.True);
            t.Rules.Resolve(t);
            Assert.That(t.Pieces[0].Placed, Is.False, "reset");
        }

        [Test]
        public void RelayBoardsGenerateUniqueAndDeducibleAndRoundTripTheirData()
        {
            var spec = new GenSpec { Elements = Element.Walls | Element.Relays, MinPieces = 3, MaxPieces = 4, RelayChance = 14 };
            AssertAccepted(spec, 12, "relays");
            for (ulong seed = 1; seed < 500; seed++)
            {
                var level = GeneratorV2.Generate(spec, seed);
                if (level == null || !level.Def.HasRelays) continue;
                // The canonical hash covers relay arms, flipped with the board.
                var board = new byte[Grid.Cells];
                var data = new byte[Grid.Cells];
                for (int i = 0; i < Grid.Height; i++)
                {
                    for (int j = 0; j < Grid.Width; j++)
                    {
                        int from = Grid.Loc(i, Grid.Width - 1 - j);
                        board[Grid.Loc(i, j)] = level.Def.BoardAt(from);
                        data[Grid.Loc(i, j)] = (byte)Canonical.FlipArms(level.Def.CellDataAt(from), true, false);
                    }
                }
                var specs = new PieceSpec[level.Def.Specs.Length];
                for (int k = 0; k < specs.Length; k++) specs[k] = Canonical.Flip(level.Def.Specs[k], true, false);
                Assert.That(Canonical.Hash(new LevelDef(board, specs, data)), Is.EqualTo(level.Hash));
                return;
            }
            Assert.Fail("no accepted board with a relay in the seed range");
        }

        [Test]
        public void ShortArmBoardsGenerateUniqueAndDeducible()
        {
            var spec = new GenSpec { Elements = Element.Walls | Element.ShortArms, MinPieces = 3, MaxPieces = 4, ShortArmChance = 12 };
            AssertAccepted(spec, 12, "short arms");
        }
    }
}
