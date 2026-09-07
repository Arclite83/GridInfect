using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    // of the ramp, so the cache memoises it, runs it ahead of time on one
    // background worker (Prefetch, in priority order), and answers a miss
    // by generating on the caller's thread, or by waiting for the worker if
    // it is already on that key. The adapter persists it (ToJson / Load) so
    // a board generated yesterday is still there today. Determinism is
    // untouched: the cache only remembers what the math would produce, so a
    // log replays to the same boards with or without it.
    public sealed class LevelCache
    {
        // Bump when the generator's output changes: every cached board is
        // then stale and the file is dropped on load.
        public const int GeneratorVersion = 1;
        public const int Capacity = 120;
        public const int MaxSeedTries = 4000;

        public static LevelCache Shared = new LevelCache();

        sealed class Entry { public PlayableLevel Level; public long Used; }
        struct Request { public int Priority; public long Order; public string Key; public GenSpec Spec; public ulong Seed; }

        readonly object _gate = new object();
        readonly Dictionary<string, Entry> _done = new Dictionary<string, Entry>(StringComparer.Ordinal);
        readonly Dictionary<string, TaskCompletionSource<PlayableLevel>> _inFlight = new Dictionary<string, TaskCompletionSource<PlayableLevel>>(StringComparer.Ordinal);
        readonly List<Request> _queue = new List<Request>();
        long _tick;
        long _order;
        bool _working;

        // Set when a level lands; the adapter clears it when it has written the file.
        public volatile bool Dirty;

        // Fired on the worker thread when a prefetched level lands (key).
        public event Action<string> Ready;

        public static string Key(GenSpec spec, ulong seed) => $"{GeneratorVersion}:{SpecId(spec)}:{seed}";

        static string SpecId(GenSpec spec)
        {
            ulong h = 14695981039346656037ul;
            foreach (char c in spec.ToJson()) { h ^= c; h *= 1099511628211ul; }
            return h.ToString("x16");
        }

        public int Count { get { lock (_gate) return _done.Count; } }
        public int Pending { get { lock (_gate) return _queue.Count + _inFlight.Count; } }

        public bool Has(GenSpec spec, ulong seed)
        {
            lock (_gate) return _done.ContainsKey(Key(spec, seed));
        }

        // The board, now. Cached: at once. In flight on the worker: wait for
        // it. Otherwise: generate here (the loader is up in front of this).
        public PlayableLevel Get(GenSpec spec, ulong seed)
        {
            string key = Key(spec, seed);
            TaskCompletionSource<PlayableLevel> tcs;
            lock (_gate)
            {
                if (_done.TryGetValue(key, out Entry entry))
                {
                    entry.Used = ++_tick;
                    return entry.Level;
                }
                if (_inFlight.TryGetValue(key, out tcs)) goto wait;
                tcs = new TaskCompletionSource<PlayableLevel>();
                _inFlight[key] = tcs;
                _queue.RemoveAll(r => r.Key == key);
            }
            Store(key, Produce(spec, seed), tcs);
            return tcs.Task.Result;
        wait:
            return tcs.Task.Result;
        }

        // Ask the worker for a board ahead of time; lower priority runs first.
        public void Prefetch(GenSpec spec, ulong seed, int priority)
        {
            string key = Key(spec, seed);
            lock (_gate)
            {
                if (_done.ContainsKey(key) || _inFlight.ContainsKey(key)) return;
                int at = _queue.FindIndex(r => r.Key == key);
                if (at >= 0)
                {
                    if (_queue[at].Priority <= priority) return;
                    _queue.RemoveAt(at);
                }
                _queue.Add(new Request { Priority = priority, Order = ++_order, Key = key, Spec = spec, Seed = seed });
                if (_working) return;
                _working = true;
            }
            Task.Run((Action)Drain);
        }

        public void CancelPrefetches()
        {
            lock (_gate) _queue.Clear();
        }

        void Drain()
        {
            while (true)
            {
                Request next;
                TaskCompletionSource<PlayableLevel> tcs;
                lock (_gate)
                {
                    if (_queue.Count == 0) { _working = false; return; }
                    int best = 0;
                    for (int n = 1; n < _queue.Count; n++)
                    {
                        if (_queue[n].Priority < _queue[best].Priority ||
                            (_queue[n].Priority == _queue[best].Priority && _queue[n].Order < _queue[best].Order)) best = n;
                    }
                    next = _queue[best];
                    _queue.RemoveAt(best);
                    if (_done.ContainsKey(next.Key) || _inFlight.ContainsKey(next.Key)) continue;
                    tcs = new TaskCompletionSource<PlayableLevel>();
                    _inFlight[next.Key] = tcs;
                }
                PlayableLevel level = null;
                try { level = Produce(next.Spec, next.Seed); }
                catch (Exception) { }
                Store(next.Key, level, tcs);
            }
        }

        static PlayableLevel Produce(GenSpec spec, ulong seed)
        {
            for (int n = 0; n < MaxSeedTries; n++)
            {
                var level = GeneratorV2.Generate(spec, seed + (ulong)n);
                if (level != null) return PlayableLevel.From(level);
            }
            return null;
        }

        void Store(string key, PlayableLevel level, TaskCompletionSource<PlayableLevel> tcs)
        {
            lock (_gate)
            {
                _inFlight.Remove(key);
                if (level != null)
                {
                    _done[key] = new Entry { Level = level, Used = ++_tick };
                    Evict();
                    Dirty = true;
                }
            }
            tcs.SetResult(level);
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
            lock (_gate) { _done.Clear(); _queue.Clear(); }
        }
    }
}
