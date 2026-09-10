using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The tutorial's contract: every step's marks win through the real
    // action pipeline, every step has exactly one solution (so the marks
    // are the only way through, and the win check is the whole gate), the
    // marks name every piece once on a cell it can go on, and the record
    // of it survives the save codec.
    [TestFixture]
    public class TutorialTests
    {
        [Test]
        public void EveryStepsMarksWinThroughTheActions()
        {
            for (int n = 0; n < TutorialLevels.Count; n++)
            {
                var d = GridInfectActions.CreateDispatcher();
                Assert.That(d.Dispatch(GridInfectActions.TutorialLoad, Inputs.Tutorial(n)).Applied, $"step {n}: load");
                Assert.That(d.State.Mode, Is.EqualTo(GameMode.Tutorial));
                Assert.That(d.State.TutorialIndex, Is.EqualTo(n));
                var s = d.State.Session;
                var marks = d.State.Solution;
                Assert.That(marks, Is.EqualTo(TutorialLevels.Get(n).Solution));
                for (int m = 0; m < marks.Length; m++)
                {
                    Assert.That(s.Solved, Is.False, $"step {n}: solved before mark {m}");
                    int i = marks[m].cell / Grid.Width, j = marks[m].cell % Grid.Width;
                    var place = d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(marks[m].piece, i, j));
                    Assert.That(place.Applied, Is.True, $"step {n} mark {m}: {place.Rejection}");
                    Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied, $"step {n} mark {m}: resolve");
                }
                Assert.That(s.Solved, Is.True, $"step {n}: not solved after its marks");
            }
        }

        [Test]
        public void EveryStepHasExactlyOneSolution()
        {
            for (int n = 0; n < TutorialLevels.Count; n++)
            {
                var step = TutorialLevels.Get(n);
                var result = SolutionCounter.Analyse(step.Def);
                Assert.That(result.Capped, Is.False, $"step {n}: capped");
                Assert.That(result.Solutions, Is.EqualTo(1), $"step {n}: solutions");
                Assert.That(result.MinPieces, Is.EqualTo(step.Def.Specs.Length), $"step {n}: every bug is needed");
                Assert.That(SolutionCounter.Wins(step.Def, step.Solution), Is.True, $"step {n}: stored order wins");
            }
        }

        [Test]
        public void MarksNameEveryPieceOnceOnAnActiveCell()
        {
            for (int n = 0; n < TutorialLevels.Count; n++)
            {
                var step = TutorialLevels.Get(n);
                var seen = new bool[step.Def.Specs.Length];
                foreach (var (piece, cell) in step.Solution)
                {
                    Assert.That(piece, Is.InRange(0, seen.Length - 1), $"step {n}: piece index");
                    Assert.That(seen[piece], Is.False, $"step {n}: piece {piece} marked twice");
                    seen[piece] = true;
                    Assert.That(step.Def.BoardAt(cell), Is.EqualTo(Cell.Active), $"step {n}: mark {cell} not on an active cell");
                }
                Assert.That(seen, Is.All.True, $"step {n}: a piece has no mark");
            }
        }

        // The sentences themselves left Core with the rest of the prose
        // (docs/I18N.md 5), and their shape — one line, non-empty, narrow
        // enough for the band that does not wrap — is checked by
        // tools/bake_strings.py over every language rather than here over
        // English only. What still belongs to Core is the count: the adapter
        // reads tut.<n>.line for n in 1..Count, so a step added here without
        // a sentence added there would be a blank board with no lesson.
        [Test]
        public void TheSeriesIsTenSteps()
        {
            Assert.That(TutorialLevels.Count, Is.EqualTo(10),
                "add or remove docs/strings/*.json tut.<n>.line to match");
        }

        [Test]
        public void ProgressIsTheHighestStepBeatenAndTheOfferIsAnsweredOnce()
        {
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(d.State.Profile.TutorialStep, Is.EqualTo(0));
            Assert.That(d.State.Profile.TutorialSeen, Is.False);

            Assert.That(d.Dispatch(GridInfectActions.TutorialSolved, Inputs.Tutorial(2)).Applied);
            Assert.That(d.State.Profile.TutorialStep, Is.EqualTo(3));
            Assert.That(d.Dispatch(GridInfectActions.TutorialSolved, Inputs.Tutorial(0)).Applied);
            Assert.That(d.State.Profile.TutorialStep, Is.EqualTo(3), "an earlier step replayed does not lower it");
            Assert.That(d.Dispatch(GridInfectActions.TutorialSolved, Inputs.Tutorial(TutorialLevels.Count)).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.TutorialLoad, Inputs.Tutorial(-1)).Applied, Is.False);

            Assert.That(d.Dispatch(GridInfectActions.TutorialSeen).Applied);
            Assert.That(d.State.Profile.TutorialSeen, Is.True);
            Assert.That(d.Dispatch(GridInfectActions.TutorialSeen).Applied, "idempotent");

            // Erasing progress keeps both: they are about knowing the game.
            Assert.That(d.Dispatch(GridInfectActions.ProgressReset).Applied);
            Assert.That(d.State.Profile.TutorialStep, Is.EqualTo(3));
            Assert.That(d.State.Profile.TutorialSeen, Is.True);
        }

        [Test]
        public void TutorialRecordRoundTripsThroughSave()
        {
            var profile = new Profile { TutorialSeen = true, TutorialStep = 4 };
            var loaded = SaveCodec.Load(SaveCodec.Save(profile));
            Assert.That(loaded.TutorialSeen, Is.True);
            Assert.That(loaded.TutorialStep, Is.EqualTo(4));

            var fresh = SaveCodec.Load(SaveCodec.Save(new Profile()));
            Assert.That(fresh.TutorialSeen, Is.False);
            Assert.That(fresh.TutorialStep, Is.EqualTo(0));

            // A v6 save has neither: the offer is still owed.
            var old = SaveCodec.Load("{\"v\":6,\"locks\":5}");
            Assert.That(old.TutorialSeen, Is.False);
            Assert.That(old.TutorialStep, Is.EqualTo(0));

            // A step beyond the series (a save from a longer tutorial) clamps.
            var beyond = SaveCodec.Load("{\"v\":7,\"tutorialStep\":99}");
            Assert.That(beyond.TutorialStep, Is.EqualTo(TutorialLevels.Count));
        }
    }
}
