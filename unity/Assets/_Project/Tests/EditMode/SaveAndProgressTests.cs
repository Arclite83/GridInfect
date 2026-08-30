using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    /// <summary>Save codec (R-501/R-502), progression policy, and free-play run recording.</summary>
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

        [Test]
        public void SaveOutputIsStable()
        {
            var profile = new Profile();
            profile.Unlocked.Add(5);
            profile.Unlocked.Add(1);
            Assert.That(SaveCodec.Save(profile), Is.EqualTo(SaveCodec.Save(profile)));
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
        public void UnknownKeysAreToleratedOnRead()
        {
            var profile = SaveCodec.Load("{\"v\":9,\"unlocked\":[3],\"futureField\":{\"x\":1},\"muted\":true}");
            Assert.That(profile.Unlocked, Is.EquivalentTo(new[] { 3 }));
            Assert.That(profile.Muted, Is.True);
        }

        [Test]
        public void UnlockPolicyMatchesModesSpec()
        {
            var profile = new Profile();
            Assert.That(Queries.IsUnlocked(profile, 0), Is.True, "level 1 always playable");
            Assert.That(Queries.IsUnlocked(profile, 1), Is.False);
            profile.Unlocked.Add(1);
            Assert.That(Queries.IsUnlocked(profile, 1), Is.True);
            Assert.That(Queries.NextClassicId(126), Is.EqualTo(127));
            Assert.That(Queries.NextClassicId(127), Is.EqualTo(-1), "no Next on the last level");
        }

        [Test]
        public void FreePlayRunRecordsBestTimeAndCount()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            Assert.That(dispatcher.Dispatch(GridInfectActions.LevelGenerate,
                Inputs.LevelGenerate(Difficulty.Beginner, seed: 9, count: 1)).Applied);
            Assert.That(dispatcher.Dispatch(GridInfectActions.FreePlayBegin, Inputs.Now(1_000)).Applied);

            SolveCurrentLevel(dispatcher);

            Assert.That(dispatcher.Dispatch(GridInfectActions.FreePlayComplete, Inputs.Now(31_000)).Applied);
            Assert.That(dispatcher.State.Profile.BestTimesMs[0], Is.EqualTo(30_000));
            Assert.That(dispatcher.State.Profile.FreePlayCounts[0], Is.EqualTo(1));
            Assert.That(dispatcher.State.Profile.Dirty, Is.True);

            // A slower second run keeps the best; count still increments.
            var second = GridInfectActions.CreateDispatcher();
            second.State.Profile.BestTimesMs[0] = 30_000;
            second.Dispatch(GridInfectActions.LevelGenerate, Inputs.LevelGenerate(Difficulty.Beginner, 9, 1));
            second.Dispatch(GridInfectActions.FreePlayBegin, Inputs.Now(0));
            SolveCurrentLevel(second);
            second.Dispatch(GridInfectActions.FreePlayComplete, Inputs.Now(45_000));
            Assert.That(second.State.Profile.BestTimesMs[0], Is.EqualTo(30_000));
        }

        [Test]
        public void BackwardClockIsRejectedAtCompletion()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            dispatcher.Dispatch(GridInfectActions.LevelGenerate, Inputs.LevelGenerate(Difficulty.Beginner, 9, 1));
            dispatcher.Dispatch(GridInfectActions.FreePlayBegin, Inputs.Now(50_000));
            SolveCurrentLevel(dispatcher);
            Assert.That(dispatcher.Dispatch(GridInfectActions.FreePlayComplete, Inputs.Now(49_999)).Applied, Is.False);
        }

        [Test]
        public void DifficultyLadderNeedsThreeCompletions()
        {
            var profile = new Profile();
            Assert.That(Queries.IsDifficultyUnlocked(profile, Difficulty.Beginner), Is.True);
            Assert.That(Queries.IsDifficultyUnlocked(profile, Difficulty.Easy), Is.False);
            Assert.That(Queries.RunsRemainingToUnlock(profile, Difficulty.Easy), Is.EqualTo(3));
            profile.FreePlayCounts[0] = 3;
            Assert.That(Queries.IsDifficultyUnlocked(profile, Difficulty.Easy), Is.True);
            Assert.That(Queries.RunsRemainingToUnlock(profile, Difficulty.Easy), Is.Zero);
        }

        [TestCase(0, "00:000")]
        [TestCase(7_123, "07:123")]
        [TestCase(67_123, "1:07:123")]
        [TestCase(59_999, "59:999")]
        [TestCase(600_000, "10:00:000")]
        public void DurationFormatMatchesOriginal(long ms, string expected)
        {
            Assert.That(Queries.FormatDuration(ms), Is.EqualTo(expected));
        }

        [Test]
        public void MissingBestTimeShowsPlaceholder()
        {
            Assert.That(Queries.FormatBestTime(0), Is.EqualTo("--:--:---"));
        }

        /// <summary>Solve the current (generated) level by replaying its sampled solution.</summary>
        static void SolveCurrentLevel(Bloodhound.Engine.Dispatcher<GameState> dispatcher)
        {
            // Regenerate with the same seed to recover the sampled solution.
            var state = dispatcher.State;
            var rng = new Bloodhound.Engine.Pcg32(9);
            LevelGenerator.Generate(state.Difficulty, ref rng, out var solution);
            for (int k = 0; k < solution.Length; k++)
            {
                var place = dispatcher.Dispatch(GridInfectActions.PiecePlace,
                    Inputs.PiecePlace(k, solution[k].i, solution[k].j));
                Assert.That(place.Applied, Is.True, place.Rejection);
                Assert.That(dispatcher.Dispatch(GridInfectActions.BoardResolve).Applied);
            }
            Assert.That(state.Session.Solved, Is.True, "sampled solution should win");
        }
    }
}
