using System;
using System.Collections.Generic;

namespace Bloodhound.Engine
{
    public sealed class ActionSchemaException : Exception
    {
        public ActionSchemaException(string message) : base(message) { }
    }

    // The one place raw payloads become domain values; failures surface as rejections.
    public readonly struct ActionInput
    {
        public static readonly Dictionary<string, object> Empty = new Dictionary<string, object>();

        readonly Dictionary<string, object> _raw;

        public ActionInput(Dictionary<string, object> raw)
        {
            _raw = raw ?? Empty;
        }

        public Dictionary<string, object> Raw => _raw ?? Empty;

        public bool Has(string key) => Raw.ContainsKey(key);

        public int Int(string key)
        {
            long v = Long(key);
            if (v < int.MinValue || v > int.MaxValue)
                throw new ActionSchemaException($"'{key}' out of int range: {v}");
            return (int)v;
        }

        public long Long(string key)
        {
            object v = Require(key);
            switch (v)
            {
                case long l: return l;
                case int i: return i;
                case double d when d == Math.Floor(d) && !double.IsInfinity(d): return (long)d;
                default:
                    throw new ActionSchemaException($"'{key}' must be an integer, got {Describe(v)}");
            }
        }

        public long LongOr(string key, long fallback) => Has(key) ? Long(key) : fallback;
        public int IntOr(string key, int fallback) => Has(key) ? Int(key) : fallback;

        public bool Bool(string key)
        {
            object v = Require(key);
            if (v is bool b) return b;
            throw new ActionSchemaException($"'{key}' must be a boolean, got {Describe(v)}");
        }

        public string Str(string key)
        {
            object v = Require(key);
            if (v is string s) return s;
            throw new ActionSchemaException($"'{key}' must be a string, got {Describe(v)}");
        }

        object Require(string key)
        {
            if (!Raw.TryGetValue(key, out object v))
                throw new ActionSchemaException($"missing required input '{key}'");
            return v;
        }

        static string Describe(object v) => v == null ? "null" : v.GetType().Name;
    }
}
