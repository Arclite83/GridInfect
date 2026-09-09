using System.Collections.Generic;
using Bloodhound.Engine;

namespace GridInfect.Core
{
    // Expand/contract: new fields are additive with read defaults; unknown keys are ignored.
    // v1: unlocked, bestMs, counts, muted. v2 (stage 3): + worlds {id: levels unlocked}.
    // v3 (stage 4): + dailyBest {date: ms}, dailyStreak, dailyLast, endlessBest[5].
    // v4 (stage 5): + locks (wallet; absent = the starting 5).
    // v5: + solved (Legacy ids) and solvedWorlds {id: [index]}. Every level
    // is open now, so what a menu draws is these, not the unlock fields; a
    // v4 save has no solved record, so one is derived from its gates on load
    // (solving N is what opened N + 1) and a returning player keeps their red.
    // v6: + skin (board colours; absent = the ship green).
    public static class SaveCodec
    {
        public const int Version = 6;

        public static string Save(Profile profile)
        {
            var unlocked = new List<object>(profile.Unlocked.Count);
            var sorted = new List<int>(profile.Unlocked);
            sorted.Sort(); // stable output: same profile, same bytes
            foreach (int id in sorted) unlocked.Add(id);

            var best = new List<object>(5);
            var counts = new List<object>(5);
            for (int d = 0; d < 5; d++)
            {
                best.Add(profile.BestTimesMs[d]);
                counts.Add(profile.FreePlayCounts[d]);
            }

            var worlds = new Dictionary<string, object>();
            var worldIds = new List<string>(profile.WorldUnlocked.Keys);
            worldIds.Sort(System.StringComparer.Ordinal);
            foreach (string id in worldIds) worlds[id] = profile.WorldUnlocked[id];

            var dailyBest = new Dictionary<string, object>();
            var dates = new List<string>(profile.DailyBestMs.Keys);
            dates.Sort(System.StringComparer.Ordinal);
            foreach (string date in dates) dailyBest[date] = profile.DailyBestMs[date];
            var endless = new List<object>(5);
            for (int g = 0; g < 5; g++) endless.Add(profile.EndlessBest[g]);

            var solved = new List<object>(profile.SolvedClassic.Count);
            var solvedIds = new List<int>(profile.SolvedClassic);
            solvedIds.Sort();
            foreach (int id in solvedIds) solved.Add(id);

            var solvedWorlds = new Dictionary<string, object>();
            var solvedWorldIds = new List<string>(profile.SolvedWorld.Keys);
            solvedWorldIds.Sort(System.StringComparer.Ordinal);
            foreach (string id in solvedWorldIds)
            {
                var indices = new List<int>(profile.SolvedWorld[id]);
                indices.Sort();
                var row = new List<object>(indices.Count);
                foreach (int index in indices) row.Add(index);
                solvedWorlds[id] = row;
            }

            return MiniJson.Write(new Dictionary<string, object>
            {
                ["v"] = Version,
                ["solved"] = solved,
                ["solvedWorlds"] = solvedWorlds,
                ["unlocked"] = unlocked,
                ["bestMs"] = best,
                ["counts"] = counts,
                ["muted"] = profile.Muted,
                ["skin"] = profile.Skin,
                ["worlds"] = worlds,
                ["dailyBest"] = dailyBest,
                ["dailyStreak"] = profile.DailyStreak,
                ["dailyLast"] = profile.DailyLastDate ?? "",
                ["endlessBest"] = endless,
                ["locks"] = profile.Locks,
            });
        }

        public static Profile Load(string json)
        {
            var profile = new Profile();
            if (string.IsNullOrEmpty(json)) return profile;

            Dictionary<string, object> root;
            try
            {
                root = MiniJson.Parse(json) as Dictionary<string, object>;
            }
            catch (System.FormatException)
            {
                return profile;
            }
            if (root == null) return profile;

            if (root.TryGetValue("unlocked", out object u) && u is List<object> unlockedList)
            {
                foreach (object item in unlockedList)
                {
                    if (item is long id && id >= 0 && id < ClassicLevels.Count)
                        profile.Unlocked.Add((int)id);
                }
            }
            ReadLongArray(root, "bestMs", profile.BestTimesMs);
            if (root.TryGetValue("counts", out object c) && c is List<object> countList)
            {
                for (int d = 0; d < 5 && d < countList.Count; d++)
                {
                    if (countList[d] is long n && n >= 0) profile.FreePlayCounts[d] = (int)n;
                }
            }
            if (root.TryGetValue("muted", out object m) && m is bool muted)
            {
                profile.Muted = muted;
            }
            if (root.TryGetValue("worlds", out object w) && w is Dictionary<string, object> worlds)
            {
                foreach (var kv in worlds)
                {
                    if (kv.Value is long n && n > 0 && Worlds.Get(kv.Key) != null) profile.WorldUnlocked[kv.Key] = (int)n;
                }
            }
            if (root.TryGetValue("dailyBest", out object db) && db is Dictionary<string, object> dailyBest)
            {
                foreach (var kv in dailyBest)
                {
                    if (kv.Value is long ms && ms > 0 && DailySpec.TryParseDate(kv.Key, out _)) profile.DailyBestMs[kv.Key] = ms;
                }
            }
            if (root.TryGetValue("dailyStreak", out object ds) && ds is long streak && streak >= 0) profile.DailyStreak = (int)streak;
            if (root.TryGetValue("dailyLast", out object dl) && dl is string last && DailySpec.TryParseDate(last, out _)) profile.DailyLastDate = last;
            if (root.TryGetValue("endlessBest", out object eb) && eb is List<object> endlessList)
            {
                for (int g = 0; g < 5 && g < endlessList.Count; g++)
                {
                    if (endlessList[g] is long n && n >= 0) profile.EndlessBest[g] = (int)n;
                }
            }
            if (root.TryGetValue("locks", out object lk) && lk is long locks && locks >= 0) profile.Locks = (int)locks;
            if (root.TryGetValue("skin", out object sk) && sk is long skin && skin >= 0 && skin < SetSkinAction.Count)
            {
                profile.Skin = (int)skin;
            }

            bool hasSolved = false;
            if (root.TryGetValue("solved", out object sv) && sv is List<object> solvedList)
            {
                hasSolved = true;
                foreach (object item in solvedList)
                {
                    if (item is long id && id >= 0 && id < ClassicLevels.Count) profile.SolvedClassic.Add((int)id);
                }
            }
            if (root.TryGetValue("solvedWorlds", out object sw) && sw is Dictionary<string, object> solvedWorlds)
            {
                hasSolved = true;
                foreach (var kv in solvedWorlds)
                {
                    World world = Worlds.Get(kv.Key);
                    if (world == null || !(kv.Value is List<object> indices)) continue;
                    foreach (object item in indices)
                    {
                        if (item is long index && index >= 0 && index < world.Count) MarkWorld(profile, kv.Key, (int)index);
                    }
                }
            }
            if (!hasSolved) DeriveSolvedFromGates(profile);
            return profile;
        }

        static void MarkWorld(Profile profile, string worldId, int index)
        {
            if (!profile.SolvedWorld.TryGetValue(worldId, out var levels))
            {
                profile.SolvedWorld[worldId] = levels = new HashSet<int>();
            }
            levels.Add(index);
        }

        // A save from before solving was recorded: read it back out of the
        // gates it did keep. Solving N unlocked N + 1, so an open id means
        // the one below it was beaten; a world open to n levels means n - 1
        // beaten, and the finished marker (n > Count) means all of them.
        static void DeriveSolvedFromGates(Profile profile)
        {
            foreach (int id in profile.Unlocked)
            {
                if (id > 0) profile.SolvedClassic.Add(id - 1);
            }
            foreach (var kv in profile.WorldUnlocked)
            {
                World world = Worlds.Get(kv.Key);
                if (world == null) continue;
                int beaten = System.Math.Min(kv.Value > world.Count ? world.Count : kv.Value - 1, world.Count);
                for (int index = 0; index < beaten; index++) MarkWorld(profile, kv.Key, index);
            }
        }

        static void ReadLongArray(Dictionary<string, object> root, string key, long[] target)
        {
            if (root.TryGetValue(key, out object v) && v is List<object> list)
            {
                for (int d = 0; d < target.Length && d < list.Count; d++)
                {
                    if (list[d] is long n && n >= 0) target[d] = n;
                }
            }
        }
    }
}
