using System;
using System.Collections.Generic;

namespace Bloodhound.Engine
{
    public sealed class ActionEntry
    {
        public int Seq;                              // 1-based position in the log
        public string Action;                        // registry name, e.g. "piece.place"
        public int Version;                          // action contract version at dispatch time
        public Dictionary<string, object> Input;     // raw payload, stored verbatim

        public string Key(Guid runId) => runId.ToString("N") + ":" + Seq;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            ["seq"] = Seq,
            ["action"] = Action,
            ["v"] = Version,
            ["input"] = Input,
        };

        public static ActionEntry FromJson(Dictionary<string, object> raw)
        {
            var input = new ActionInput(raw);
            var payload = raw.TryGetValue("input", out object p) && p is Dictionary<string, object> d
                ? d
                : new Dictionary<string, object>();
            return new ActionEntry
            {
                Seq = input.Int("seq"),
                Action = input.Str("action"),
                Version = input.Int("v"),
                Input = payload,
            };
        }
    }

    // Append-only, inputs stored verbatim — the load-bearing record: replay,
    // audit, and sync all read this one structure.
    public sealed class ActionLog
    {
        readonly List<ActionEntry> _entries = new List<ActionEntry>();

        public Guid RunId { get; private set; } = Guid.NewGuid();

        public int Count => _entries.Count;
        public ActionEntry this[int index] => _entries[index];
        public IReadOnlyList<ActionEntry> Entries => _entries;

        public ActionEntry Append(string action, int version, Dictionary<string, object> input)
        {
            var entry = new ActionEntry
            {
                Seq = _entries.Count + 1,
                Action = action,
                Version = version,
                Input = input ?? new Dictionary<string, object>(),
            };
            _entries.Add(entry);
            return entry;
        }

        public void Clear()
        {
            _entries.Clear();
            RunId = Guid.NewGuid();
        }

        public string ToJson()
        {
            var entries = new List<object>(_entries.Count);
            foreach (var e in _entries) entries.Add(e.ToJson());
            return MiniJson.Write(new Dictionary<string, object>
            {
                ["v"] = 1,
                ["run"] = RunId.ToString("N"),
                ["entries"] = entries,
            });
        }

        public static List<ActionEntry> ParseEntries(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>
                       ?? throw new FormatException("action log: root must be an object");
            if (!(root.TryGetValue("entries", out object raw) && raw is List<object> list))
                throw new FormatException("action log: missing 'entries'");
            var result = new List<ActionEntry>(list.Count);
            foreach (object item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    throw new FormatException("action log: entry must be an object");
                result.Add(ActionEntry.FromJson(d));
            }
            return result;
        }
    }
}
