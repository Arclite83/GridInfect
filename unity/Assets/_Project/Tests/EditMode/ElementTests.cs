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
                Assert.That(SolutionCounter.Count(level.Def), Is.EqualTo(1), $"{what} seed {seed}: unique");
                var solve = Deducer.Solve(level.Def);
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

        [Test]
        public void ShortArmBoardsGenerateUniqueAndDeducible()
        {
            var spec = new GenSpec { Elements = Element.Walls | Element.ShortArms, MinPieces = 3, MaxPieces = 4, ShortArmChance = 12 };
            AssertAccepted(spec, 12, "short arms");
        }
    }
}
