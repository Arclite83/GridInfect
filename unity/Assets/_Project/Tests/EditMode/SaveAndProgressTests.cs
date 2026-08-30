using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Two verticals: player progress survives the save codec (and corruption
    // yields a fresh profile, never a crash loop), and a full Free Play run
    // lands its record through the action pipeline end to end.
    [TestFixture]
    public class SaveAndProgressTests
    {
        [Test]
        public void ProfileRoundTripsThroughSave()
        {
            var profile = new Profile { Muted = true };
            profile.Unlocked.Add(1);
            profile.Unlocked.Add(64);
            profile.BestTimesMs[2] = 61234;
            profile.FreePlayCounts[0] = 3;

            var loaded = SaveCodec.Load(SaveCodec.Save(profile));

            Assert.That(loaded.Unlocked, Is.EquivalentTo(profile.Unlocked));
            Assert.That(loaded.BestTimesMs, Is.EqualTo(profile.BestTimesMs));
            Assert.That(loaded.FreePlayCounts, Is.EqualTo(profile.FreePlayCounts));
            Assert.That(loaded.Muted, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not json at all")]
        [TestCase("[1,2,3]")]
        [TestCase("{\"unlocked\":\"nope\",\"bestMs\":true}")]
        public void CorruptSaveYieldsFreshProfile(string bad)
        {
            var profile = SaveCodec.Load(bad);
            Assert.That(profile.Unlocked, Is.Empty);
            Assert.That(profile.Muted, Is.False);
        }

        [Test]
        public void FreePlayRunRecordsBestTimeAndCount()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            Assert.That(dispatcher.Dispatch(GridInfectActions.LevelGenerate,
                Inputs.LevelGenerate(Difficulty.Beginner, seed: 9, count: 1)).Applied);
            Assert.That(dispatcher.Dispatch(GridInfectActions.FreePlayBegin, Inputs.Now(1_000)).Applied);

            SolveCurrentLevel(dispatcher, seed: 9);

            Assert.That(dispatcher.Dispatch(GridInfectActions.FreePlayComplete, Inputs.Now(31_000)).Applied);
            Assert.That(dispatcher.State.Profile.BestTimesMs[0], Is.EqualTo(30_000));
            Assert.That(dispatcher.State.Profile.FreePlayCounts[0], Is.EqualTo(1));
            Assert.That(dispatcher.State.Profile.Dirty, Is.True);

            var second = GridInfectActions.CreateDispatcher();
            second.State.Profile.BestTimesMs[0] = 30_000;
            second.Dispatch(GridInfectActions.LevelGenerate, Inputs.LevelGenerate(Difficulty.Beginner, 9, 1));
            second.Dispatch(GridInfectActions.FreePlayBegin, Inputs.Now(0));
            SolveCurrentLevel(second, seed: 9);
            second.Dispatch(GridInfectActions.FreePlayComplete, Inputs.Now(45_000));
            Assert.That(second.State.Profile.BestTimesMs[0], Is.EqualTo(30_000), "slower run keeps the best");
        }

        static void SolveCurrentLevel(Bloodhound.Engine.Dispatcher<GameState> dispatcher, ulong seed)
        {
            var rng = new Bloodhound.Engine.Pcg32(seed);
            LevelGenerator.Generate(dispatcher.State.Difficulty, ref rng, out var solution);
            for (int k = 0; k < solution.Length; k++)
            {
                var place = dispatcher.Dispatch(GridInfectActions.PiecePlace,
                    Inputs.PiecePlace(k, solution[k].i, solution[k].j));
                Assert.That(place.Applied, Is.True, place.Rejection);
                Assert.That(dispatcher.Dispatch(GridInfectActions.BoardResolve).Applied);
            }
            Assert.That(dispatcher.State.Session.Solved, Is.True);
        }
    }
}
