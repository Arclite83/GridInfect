using System.Collections.Generic;
using System.IO;
using Bloodhound.Engine;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Stage 3 acceptance: every baked world level loads through world.load
    // and solves through the action pipeline with its stored solution; a
    // sample regenerates identically from its recorded seed and spec;
    // progression unlocks level N+1 and the next world and replays from the
    // log; the save schema bump migrates a v1 file.
    [TestFixture]
    public class WorldTests
    {
        [Test]
        public void LaunchContentHasTheAgreedShape()
        {
            Assert.That(Worlds.Count, Is.GreaterThanOrEqualTo(12));
            foreach (World w in Worlds.All)
            {
                Assert.That(w.Count, Is.InRange(20, 25), w.Id);
                Assert.That(w.Elements, Does.Contain("walls"), w.Id);
                for (int n = 1; n < w.Count; n++)
                {
                    Assert.That(Worlds.Grade(w.Id, n), Is.GreaterThanOrEqualTo(Worlds.Grade(w.Id, n - 1)),
                        $"{w.Id}: grades must not fall within a world");
                }
            }
            // The twelve launch worlds ramp; the element worlds after them
            // (one per element, stages 8-12) each start their own ramp.
            for (int n = 1; n < 12 && n < Worlds.Count; n++)
            {
                Assert.That(Worlds.Grade(Worlds.All[n].Id, 0), Is.GreaterThanOrEqualTo(Worlds.Grade(Worlds.All[n - 1].Id, 0)),
                    "grades ramp across the launch worlds");
            }
        }

        [Test]
        public void EveryLevelLoadsAndSolvesWithItsStoredSolution()
        {
            var hashes = new HashSet<string>();
            foreach (World w in Worlds.All)
            {
                for (int n = 0; n < w.Count; n++)
                {
                    var dispatcher = GridInfectActions.CreateDispatcher();
                    Assert.That(dispatcher.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, n)).Applied, $"{w.Id}/{n}");
                    Assert.That(dispatcher.State.Mode, Is.EqualTo(GameMode.World));
                    foreach (var (piece, cell) in Worlds.Solution(w.Id, n))
                    {
                        if (dispatcher.State.Session.Pieces[piece].Locked) continue;   // placed by the loader
                        var place = dispatcher.Dispatch(GridInfectActions.PiecePlace,
                            Inputs.PiecePlace(piece, cell / Grid.Width, cell % Grid.Width));
                        Assert.That(place.Applied, Is.True, $"{w.Id}/{n}: {place.Rejection}");
                        Assert.That(dispatcher.Dispatch(GridInfectActions.BoardResolve).Applied);
                    }
                    Assert.That(dispatcher.State.Session.Solved, Is.True, $"{w.Id}/{n}: stored solution does not win");
                    Assert.That(hashes.Add(Worlds.Hash(w.Id, n)), Is.True, $"{w.Id}/{n}: duplicate level across worlds");
                }
            }
        }

        [Test]
        public void EveryLevelIsUniqueAndDeducible()
        {
            foreach (World w in Worlds.All)
            {
                for (int n = 0; n < w.Count; n++)
                {
                    var def = Worlds.Level(w.Id, n);
                    var placed = Worlds.Placed(w.Id, n);
                    Assert.That(SolutionCounter.Count(def, placed), Is.EqualTo(1), $"{w.Id}/{n}");
                    var solve = Deducer.Solve(def, placed);
                    Assert.That(solve.Solved && solve.Guesses == 0, Is.True, $"{w.Id}/{n}");
                    Assert.That((int)Grader.Grade(solve, def), Is.EqualTo(Worlds.Grade(w.Id, n)), $"{w.Id}/{n}: baked grade");
                }
            }
        }

        // The first two levels of each world regenerate from the seed and the
        // spec recorded in the world's JSONL header.
        [Test]
        public void WorldsRegenerateFromTheirRecordedSeeds()
        {
            foreach (World w in Worlds.All)
            {
                string path = Path.Combine(TestPaths.RepoRoot, "docs", "worlds", w.Id + ".jsonl");
                string header;
                using (var reader = new StreamReader(path)) header = reader.ReadLine();
                var root = (Dictionary<string, object>)MiniJson.Parse(header);
                var world = (Dictionary<string, object>)root["world"];
                var spec = GenSpec.FromJson((Dictionary<string, object>)world["spec"]);
                for (int n = 0; n < 2 && n < w.Count; n++)
                {
                    var level = GeneratorV2.Generate(spec, Worlds.Seed(w.Id, n));
                    Assert.That(level, Is.Not.Null, $"{w.Id}/{n}: seed {Worlds.Seed(w.Id, n)} no longer accepted");
                    Assert.That(level.Hash, Is.EqualTo(Worlds.Hash(w.Id, n)), $"{w.Id}/{n}: board changed");
                    Assert.That((int)level.Grade, Is.EqualTo(Worlds.Grade(w.Id, n)), $"{w.Id}/{n}: grade changed");
                }
            }
        }

        [Test]
        public void ProgressionUnlocksTheNextLevelAndTheNextWorld()
        {
            var d = GridInfectActions.CreateDispatcher();
            var profile = d.State.Profile;
            World first = Worlds.First, second = Worlds.All[1];

            Assert.That(Queries.IsWorldLevelUnlocked(profile, first.Id, 0), Is.True, "first level of the first world is always open");
            Assert.That(Queries.IsWorldLevelUnlocked(profile, first.Id, 1), Is.False);
            Assert.That(Queries.IsWorldUnlocked(profile, second.Id), Is.False);

            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(first.Id, 1)).Applied);
            Assert.That(Queries.IsWorldLevelUnlocked(profile, first.Id, 1), Is.True);
            Assert.That(Queries.IsWorldLevelUnlocked(profile, first.Id, 2), Is.False);

            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(first.Id, first.Count)).Applied);
            Assert.That(Queries.IsWorldFinished(profile, first.Id), Is.True);
            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(first.Id, first.Count + 1)).Applied, Is.False);

            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorld, Inputs.UnlockWorld(second.Id)).Applied);
            Assert.That(Queries.IsWorldLevelUnlocked(profile, second.Id, 0), Is.True);
            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorld, Inputs.UnlockWorld("nope")).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(second.Id, second.Count)).Applied, Is.False);

            var replayed = GridInfectActions.CreateDispatcher();
            replayed.Replay(d.Log.Entries);
            Assert.That(replayed.State.Profile.WorldUnlocked, Is.EqualTo(profile.WorldUnlocked));
        }

        [Test]
        public void SaveMigratesFromV1AndRoundTripsV2()
        {
            string v1 = "{\"v\":1,\"unlocked\":[1,2],\"bestMs\":[0,5000,0,0,0],\"counts\":[0,1,0,0,0],\"muted\":true}";
            var migrated = SaveCodec.Load(v1);
            Assert.That(migrated.Unlocked, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(migrated.BestTimesMs[1], Is.EqualTo(5000));
            Assert.That(migrated.Muted, Is.True);
            Assert.That(migrated.WorldUnlocked, Is.Empty);
            Assert.That(Queries.IsWorldLevelUnlocked(migrated, Worlds.First.Id, 0), Is.True);

            migrated.WorldUnlocked[Worlds.First.Id] = 7;
            migrated.WorldUnlocked[Worlds.All[1].Id] = 1;
            string json = SaveCodec.Save(migrated);
            Assert.That(json, Does.Contain("\"v\":" + SaveCodec.Version));
            var loaded = SaveCodec.Load(json);
            Assert.That(loaded.WorldUnlocked, Is.EqualTo(migrated.WorldUnlocked));
            Assert.That(SaveCodec.Save(loaded), Is.EqualTo(json), "stable bytes");

            var unknown = SaveCodec.Load("{\"v\":2,\"worlds\":{\"ghost\":3,\"" + Worlds.First.Id + "\":2}}");
            Assert.That(unknown.WorldUnlocked.ContainsKey("ghost"), Is.False, "unknown world ids are dropped");
            Assert.That(unknown.WorldUnlocked[Worlds.First.Id], Is.EqualTo(2));
        }
    }
}
