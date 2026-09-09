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
        public void SolvedLevelsRoundTripThroughSave()
        {
            var profile = new Profile();
            profile.SolvedClassic.Add(0);
            profile.SolvedClassic.Add(31);
            string worldId = Worlds.First.Id;
            profile.SolvedWorld[worldId] = new System.Collections.Generic.HashSet<int> { 0, 2 };

            var loaded = SaveCodec.Load(SaveCodec.Save(profile));

            Assert.That(loaded.SolvedClassic, Is.EquivalentTo(new[] { 0, 31 }));
            Assert.That(loaded.SolvedWorld[worldId], Is.EquivalentTo(new[] { 0, 2 }));
            Assert.That(Queries.WorldLevelsSolved(loaded, worldId), Is.EqualTo(2));
        }

        // A save written before solving was recorded still has to show a
        // returning player their red: solving N is what opened N + 1, so the
        // gates the old save did keep say what was beaten.
        [Test]
        public void V4SaveDerivesSolvedFromItsGates()
        {
            string worldId = Worlds.First.Id;
            string v4 = "{\"v\":4,\"unlocked\":[1,2,3],\"bestMs\":[0,0,0,0,0],\"counts\":[0,0,0,0,0]," +
                        "\"muted\":false,\"worlds\":{\"" + worldId + "\":3}}";

            var migrated = SaveCodec.Load(v4);

            Assert.That(migrated.SolvedClassic, Is.EquivalentTo(new[] { 0, 1, 2 }));
            Assert.That(migrated.SolvedWorld[worldId], Is.EquivalentTo(new[] { 0, 1 }));
        }

        // The finished marker is Count + 1, which means every level beaten.
        [Test]
        public void V4FinishedWorldMigratesToEveryLevelSolved()
        {
            World world = Worlds.First;
            string v4 = "{\"v\":4,\"worlds\":{\"" + world.Id + "\":" + (world.Count + 1) + "}}";

            var migrated = SaveCodec.Load(v4);

            Assert.That(Queries.WorldLevelsSolved(migrated, world.Id), Is.EqualTo(world.Count));
            Assert.That(Queries.IsWorldSolved(migrated, world.Id), Is.True);
            Assert.That(Queries.WorldInfection(migrated, world.Id), Is.EqualTo(1f));
        }

        // A v5 save with nothing solved is a real record, not a missing one:
        // it must not fall back to deriving from the gates it still carries.
        [Test]
        public void EmptySolvedRecordIsNotRederived()
        {
            string worldId = Worlds.First.Id;
            string v5 = "{\"v\":5,\"unlocked\":[1,2],\"solved\":[],\"solvedWorlds\":{}," +
                        "\"worlds\":{\"" + worldId + "\":3}}";

            var loaded = SaveCodec.Load(v5);

            Assert.That(loaded.SolvedClassic, Is.Empty);
            Assert.That(loaded.SolvedWorld, Is.Empty);
        }

        [Test]
        public void SolveActionsRecordProgressAndResetClearsIt()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            string worldId = Worlds.First.Id;
            Assert.That(dispatcher.Dispatch(GridInfectActions.ProgressSolved, Inputs.Solved(4)).Applied);
            Assert.That(dispatcher.Dispatch(GridInfectActions.ProgressSolvedWorld, Inputs.SolvedWorld(worldId, 1)).Applied);
            var profile = dispatcher.State.Profile;
            profile.DailyBestMs["2026-09-07"] = 61_000;
            profile.EndlessBest[1] = 4;

            Assert.That(Queries.IsClassicSolved(profile, 4), Is.True);
            Assert.That(Queries.IsWorldLevelSolved(profile, worldId, 1), Is.True);

            int locks = profile.Locks;
            Assert.That(dispatcher.Dispatch(GridInfectActions.ProgressReset).Applied);

            Assert.That(profile.SolvedClassic, Is.Empty);
            Assert.That(profile.SolvedWorld, Is.Empty);
            Assert.That(profile.DailyBestMs, Is.Empty);
            Assert.That(profile.EndlessBest[1], Is.EqualTo(0));
            Assert.That(profile.Locks, Is.EqualTo(locks), "the wallet is earned, not progress");
        }

        [Test]
        public void SolveActionsRejectOutOfRange()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            Assert.That(dispatcher.Dispatch(GridInfectActions.ProgressSolved, Inputs.Solved(-1)).Applied, Is.False);
            Assert.That(dispatcher.Dispatch(GridInfectActions.ProgressSolved,
                Inputs.Solved(ClassicLevels.Count)).Applied, Is.False);
            Assert.That(dispatcher.Dispatch(GridInfectActions.ProgressSolvedWorld,
                Inputs.SolvedWorld("nope", 0)).Applied, Is.False);
            Assert.That(dispatcher.Dispatch(GridInfectActions.ProgressSolvedWorld,
                Inputs.SolvedWorld(Worlds.First.Id, Worlds.First.Count)).Applied, Is.False);
        }

        // The action that loads an Endless board, the warmer that generates
        // it ahead of time and the loading card that waits for it all name a
        // seed. If they ever disagreed the card would wait on a board nobody
        // was making, so the arithmetic is one method and this pins it.
        [Test]
        public void EndlessLevelSeedsFollowTheRunStride()
        {
            var run = new EndlessRun { Seed = 4_242, Index = 3 };

            Assert.That(run.SeedAt(0), Is.EqualTo(4_242ul));
            Assert.That(run.SeedAt(1), Is.EqualTo(4_242ul + EndlessRun.Stride));
            Assert.That(run.NextSeed, Is.EqualTo(run.SeedAt(4)));
            Assert.That(EndlessRun.Stride, Is.GreaterThan((ulong)LevelCache.MaxSeedTries),
                "two levels of a run must never scan into each other");
        }

        // A run advanced n times must land on the seed the warmer had been
        // generating for that index all along.
        [Test]
        public void EndlessAdvanceLandsOnTheWarmedSeed()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            Assert.That(dispatcher.Dispatch(GridInfectActions.EndlessBegin,
                Inputs.EndlessBegin(Solving.Grade.G1, 777)).Applied);
            var run = dispatcher.State.EndlessRun;
            ulong expected = run.NextSeed;

            SolveCurrentEndlessLevel(dispatcher);
            Assert.That(dispatcher.Dispatch(GridInfectActions.EndlessAdvance).Applied);

            Assert.That(dispatcher.State.EndlessRun.Index, Is.EqualTo(1));
            Assert.That(dispatcher.State.EndlessRun.SeedAt(1), Is.EqualTo(expected));
            Assert.That(dispatcher.State.EndlessRun.LevelSeed, Is.GreaterThanOrEqualTo(expected));
        }

        static void SolveCurrentEndlessLevel(Bloodhound.Engine.Dispatcher<GameState> dispatcher)
        {
            foreach (var (piece, cell) in dispatcher.State.Solution)
            {
                dispatcher.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(piece, cell / Grid.Width, cell % Grid.Width));
                dispatcher.Dispatch(GridInfectActions.BoardResolve);
            }
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

        [Test]
        public void SaveMigratesFromV2AndRoundTripsV3()
        {
            string v2 = "{\"v\":2,\"unlocked\":[1],\"bestMs\":[0,0,0,0,0],\"counts\":[0,0,0,0,0],\"muted\":false,\"worlds\":{\"w01\":3}}";
            var migrated = SaveCodec.Load(v2);
            Assert.That(migrated.WorldUnlocked["w01"], Is.EqualTo(3));
            Assert.That(migrated.DailyStreak, Is.EqualTo(0));
            Assert.That(migrated.DailyBestMs, Is.Empty);

            migrated.DailyBestMs["2026-09-07"] = 61_000;
            migrated.DailyStreak = 3;
            migrated.DailyLastDate = "2026-09-07";
            migrated.EndlessBest[2] = 9;
            string json = SaveCodec.Save(migrated);
            Assert.That(json, Does.Contain("\"v\":" + SaveCodec.Version));
            var loaded = SaveCodec.Load(json);
            Assert.That(loaded.DailyBestMs, Is.EqualTo(migrated.DailyBestMs));
            Assert.That(loaded.DailyStreak, Is.EqualTo(3));
            Assert.That(loaded.DailyLastDate, Is.EqualTo("2026-09-07"));
            Assert.That(loaded.EndlessBest, Is.EqualTo(migrated.EndlessBest));
            Assert.That(SaveCodec.Save(loaded), Is.EqualTo(json));
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
