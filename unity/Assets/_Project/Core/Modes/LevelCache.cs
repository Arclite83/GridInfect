using System;
using System.Collections.Generic;
using Bloodhound.Engine;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // A generated level as a loader needs it: the board, its stored solution
    // (locked pieces first), its locks and the stats the modes read. Lighter
    // than GeneratedLevel (no trace), so it round-trips through the cache file.
    public sealed class PlayableLevel
    {
        public LevelDef Def;
        public (int piece, int cell)[] Solution;
        public (int piece, int cell)[] Locks;
        public Grade Grade;
        public int TraceLength;
        public ulong Seed;             // the accepted seed
        public string Hash;

        public static PlayableLevel From(GeneratedLevel g) => new PlayableLevel
        {
            Def = g.Def, Solution = g.Solution, Locks = g.Locks, Grade = g.Grade,
            TraceLength = g.Trace.Length, Seed = g.Seed, Hash = g.Hash,
        };
    }

    // The on-device level cache. Every generated mode's board is a pure
    // function of (spec, seed): the first seed at or after `seed` the
    // generator accepts. That function is seconds of solver work at the top
    // of the ramp, so the cache memoises it, has the kernel's Work run it
    // ahead of time (Prefetch, in priority order, one job at a time so the
    // order means something), and answers a miss by generating on the
    // caller's thread, or by waiting for the job that is already on it.
    // Each scan fans its seed range across the cores (Work.Map) and takes
    // the lowest accepted seed, so the answer does not depend on the core
    // count. The adapter persists it (ToJson / Load) so a board generated
    // yesterday is still there today. Determinism is untouched: the cache
    // only remembers what the math would produce, so a log replays to the
    // same boards with or without it.
    public sealed class LevelCache
    {
        // Bump when the generator's output changes: every cached board is
        // then stale and the file is dropped on load.
        public const int GeneratorVersion = 1;
        public const int Capacity = 120;
        public const int MaxSeedTries = 4000;

        public static LevelCache Shared = new LevelCache(Work.Shared);

        sealed class Entry { public PlayableLevel Level; public long Used; }

        readonly Work _work;
        readonly object _gate = new object();
        readonly Dictionary<string, Entry> _done = new Dictionary<string, Entry>(StringComparer.Ordinal);
        readonly Dictionary<string, WorkItem<PlayableLevel>> _inFlight = new Dictionary<string, WorkItem<PlayableLevel>>(StringComparer.Ordinal);
        readonly HashSet<string> _inline = new HashSet<string>(StringComparer.Ordinal);
        long _tick;

        // Set when a level lands; the adapter clears it when it has written the file.
        public volatile bool Dirty;

        // Fired on the thread the level landed on (key).
        public event Action<string> Ready;

        public LevelCache(Work work = null)
        {
            _work = work ?? Work.Shared;
        }

        public static string Key(GenSpec spec, ulong seed) => $"{GeneratorVersion}:{SpecId(spec)}:{seed}";

        static string SpecId(GenSpec spec)
        {
            ulong h = 14695981039346656037ul;
            foreach (char c in spec.ToJson()) { h ^= c; h *= 1099511628211ul; }
            return h.ToString("x16");
        }

        public int Count { get { lock (_gate) return _done.Count; } }
        public int Pending { get { lock (_gate) return _inFlight.Count + _inline.Count; } }

        public bool Has(GenSpec spec, ulong seed)
        {
            lock (_gate) return _done.ContainsKey(Key(spec, seed));
        }

        // The board, now. Cached: at once. Running on a worker: wait for it.
        // Still queued behind other work: taken back and generated here.
        // Otherwise: generated here (the loader is up in front of this).
        public PlayableLevel Get(GenSpec spec, ulong seed)
        {
            string key = Key(spec, seed);
            WorkItem<PlayableLevel> running = null;
            lock (_gate)
            {
                if (_done.TryGetValue(key, out Entry entry))
                {
                    entry.Used = ++_tick;
                    return entry.Level;
                }
                if (_inFlight.TryGetValue(key, out WorkItem<PlayableLevel> item))
                {
                    if (_work.Cancel(key)) _inFlight.Remove(key);
                    else running = item;
                }
                if (running == null) _inline.Add(key);
            }
            if (running != null)
            {
                try { return running.Wait(); }
                catch (Exception) { lock (_gate) _inline.Add(key); }
            }
            PlayableLevel level = Produce(spec, seed);
            Store(key, level);
            return level;
        }

        // Ask for a board ahead of time; lower priority runs first.
        public void Prefetch(GenSpec spec, ulong seed, int priority)
        {
            string key = Key(spec, seed);
            lock (_gate)
            {
                if (_done.ContainsKey(key) || _inline.Contains(key)) return;
                if (_inFlight.ContainsKey(key))
                {
                    _work.Raise(key, priority);
                    return;
                }
                _inFlight[key] = _work.Run(() =>
                {
                    PlayableLevel level = Produce(spec, seed);
                    Store(key, level);
                    return level;
                }, priority, key);
            }
        }

        // Takes back every board still waiting in the queue.
        public void CancelPrefetches()
        {
            lock (_gate)
            {
                var keys = new List<string>(_inFlight.Keys);
                foreach (string key in keys)
                {
                    if (_work.Cancel(key)) _inFlight.Remove(key);
                }
            }
        }

        // The widest scan chunk, in seeds per core. Seed cost varies by an
        // order of magnitude, so one seed per core idles the fast cores
        // while the slow seed finishes; a wide chunk evens that out at the
        // price of seeds generated past the winner. The scan starts at one
        // per core and doubles each round, so a spec that accepts early
        // pays for a handful of seeds and only the long scans go wide.
        public int MaxChunkPerCore = 8;

        // The first accepted seed at or after `seed`: the range is scanned a
        // chunk of seeds at a time across the cores, and the lowest accepted
        // seed in the chunk wins, so the board is the same on one core or eight.
        PlayableLevel Produce(GenSpec spec, ulong seed)
        {
            int cores = Math.Max(1, _work.Parallelism);
            int widest = cores * Math.Max(1, MaxChunkPerCore);
            var results = new GeneratedLevel[widest];
            int chunk = cores;
            int start = 0;
            while (start < MaxSeedTries)
            {
                int n = Math.Min(chunk, MaxSeedTries - start);
                ulong first = seed + (ulong)start;
                _work.Map(n, i => results[i] = GeneratorV2.Generate(spec, first + (ulong)i));
                for (int i = 0; i < n; i++)
                {
                    if (results[i] != null) return PlayableLevel.From(results[i]);
                }
                start += n;
                chunk = Math.Min(chunk * 2, widest);
            }
            return null;
        }

        void Store(string key, PlayableLevel level)
        {
            lock (_gate)
            {
                _inFlight.Remove(key);
                _inline.Remove(key);
                if (level != null)
                {
                    _done[key] = new Entry { Level = level, Used = ++_tick };
                    Evict();
                    Dirty = true;
                }
            }
            if (level != null) Ready?.Invoke(key);
        }

        void Evict()
        {
            while (_done.Count > Capacity)
            {
                string oldest = null;
                long used = long.MaxValue;
                foreach (var kv in _done)
                {
                    if (kv.Value.Used < used) { used = kv.Value.Used; oldest = kv.Key; }
                }
                _done.Remove(oldest);
            }
        }

        // ---- persistence: {"v":1,"gen":N,"tick":T,"levels":[{...}]} ----

        public string ToJson()
        {
            var levels = new List<object>();
            lock (_gate)
            {
                foreach (var kv in _done)
                {
                    var l = kv.Value.Level;
                    levels.Add(new Dictionary<string, object>
                    {
                        ["k"] = kv.Key,
                        ["u"] = kv.Value.Used,
                        ["seed"] = (long)l.Seed,
                        ["grade"] = (long)(int)l.Grade,
                        ["trace"] = (long)l.TraceLength,
                        ["hash"] = l.Hash,
                        ["board"] = LevelPools.BoardText(l.Def),
                        ["pieces"] = LevelPools.PiecesText(l.Def),
                        ["relays"] = LevelPools.RelaysText(l.Def),
                        ["solution"] = LevelPools.PairsText(l.Solution),
                        ["locks"] = LevelPools.PairsText(l.Locks),
                    });
                }
                return MiniJson.Write(new Dictionary<string, object>
                {
                    ["v"] = 1L, ["gen"] = (long)GeneratorVersion, ["tick"] = _tick, ["levels"] = levels,
                });
            }
        }

        // Loads a file written by ToJson; anything unreadable or from another
        // generator version is ignored (the cache simply starts empty).
        public void Load(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            Dictionary<string, object> root;
            try { root = MiniJson.Parse(json) as Dictionary<string, object>; }
            catch (Exception) { return; }
            if (root == null) return;
            if (!(root.TryGetValue("gen", out object gen) && gen is long g && g == GeneratorVersion)) return;
            if (!(root.TryGetValue("levels", out object list) && list is List<object> levels)) return;
            lock (_gate)
            {
                if (root.TryGetValue("tick", out object tick) && tick is long t) _tick = Math.Max(_tick, t);
                foreach (object item in levels)
                {
                    if (!(item is Dictionary<string, object> d)) continue;
                    try
                    {
                        string key = (string)d["k"];
                        if (!key.StartsWith(GeneratorVersion + ":", StringComparison.Ordinal)) continue;
                        var level = new PlayableLevel
                        {
                            Def = LevelPools.Decode((string)d["board"], (string)d["pieces"], d.TryGetValue("relays", out object r) ? (string)r : ""),
                            Solution = LevelPools.Pairs((string)d["solution"]),
                            Locks = LevelPools.Pairs(d.TryGetValue("locks", out object lk) ? (string)lk : ""),
                            Grade = (Grade)(int)(long)d["grade"],
                            TraceLength = (int)(long)d["trace"],
                            Seed = (ulong)(long)d["seed"],
                            Hash = (string)d["hash"],
                        };
                        _done[key] = new Entry { Level = level, Used = d.TryGetValue("u", out object u) && u is long used ? used : 0 };
                    }
                    catch (Exception) { }
                }
                Evict();
            }
        }

        public void Clear()
        {
            lock (_gate) _done.Clear();
        }
    }
}
