using System.Collections.Generic;
using Bloodhound.Engine;

namespace GridInfect.Core
{
    // Expand/contract: new fields are additive with read defaults; unknown keys are ignored.
    // v1: unlocked, bestMs, counts, muted. v2 (stage 3): + worlds {id: levels unlocked}.
    public static class SaveCodec
    {
        public const int Version = 2;

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

            return MiniJson.Write(new Dictionary<string, object>
            {
                ["v"] = Version,
                ["unlocked"] = unlocked,
                ["bestMs"] = best,
                ["counts"] = counts,
                ["muted"] = profile.Muted,
                ["worlds"] = worlds,
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
            return profile;
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
