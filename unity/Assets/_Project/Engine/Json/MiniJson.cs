using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bloodhound.Engine
{
    /// <summary>
    /// Minimal, dependency-free JSON parser and writer.
    ///
    /// Owned by the kernel because JSON is the kernel's boundary format: action
    /// log entries, save files, and test fixtures all pass through it. Values
    /// map to: object -> Dictionary&lt;string, object&gt;, array -> List&lt;object&gt;,
    /// string -> string, number -> long (when integral) or double, true/false ->
    /// bool, null -> null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            int pos = 0;
            object value = ParseValue(text, ref pos);
            SkipWhitespace(text, ref pos);
            if (pos != text.Length)
                throw new FormatException($"JSON: trailing content at index {pos}");
            return value;
        }

        public static string Write(object value)
        {
            var sb = new StringBuilder(256);
            WriteValue(sb, value);
            return sb.ToString();
        }

        // ---- parsing ----

        static object ParseValue(string s, ref int pos)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) throw new FormatException("JSON: unexpected end of input");
            char c = s[pos];
            switch (c)
            {
                case '{': return ParseObject(s, ref pos);
                case '[': return ParseArray(s, ref pos);
                case '"': return ParseString(s, ref pos);
                case 't': Expect(s, ref pos, "true"); return true;
                case 'f': Expect(s, ref pos, "false"); return false;
                case 'n': Expect(s, ref pos, "null"); return null;
                default: return ParseNumber(s, ref pos);
            }
        }

        static Dictionary<string, object> ParseObject(string s, ref int pos)
        {
            var result = new Dictionary<string, object>();
            pos++; // '{'
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == '}') { pos++; return result; }
            while (true)
            {
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length || s[pos] != '"')
                    throw new FormatException($"JSON: expected object key at index {pos}");
                string key = ParseString(s, ref pos);
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length || s[pos] != ':')
                    throw new FormatException($"JSON: expected ':' at index {pos}");
                pos++;
                result[key] = ParseValue(s, ref pos);
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) throw new FormatException("JSON: unterminated object");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == '}') { pos++; return result; }
                throw new FormatException($"JSON: expected ',' or '}}' at index {pos}");
            }
        }

        static List<object> ParseArray(string s, ref int pos)
        {
            var result = new List<object>();
            pos++; // '['
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == ']') { pos++; return result; }
            while (true)
            {
                result.Add(ParseValue(s, ref pos));
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) throw new FormatException("JSON: unterminated array");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == ']') { pos++; return result; }
                throw new FormatException($"JSON: expected ',' or ']' at index {pos}");
            }
        }

        static string ParseString(string s, ref int pos)
        {
            pos++; // '"'
            var sb = new StringBuilder();
            while (true)
            {
                if (pos >= s.Length) throw new FormatException("JSON: unterminated string");
                char c = s[pos++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (pos >= s.Length) throw new FormatException("JSON: unterminated escape");
                char e = s[pos++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (pos + 4 > s.Length) throw new FormatException("JSON: bad \\u escape");
                        sb.Append((char)ushort.Parse(s.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        pos += 4;
                        break;
                    default: throw new FormatException($"JSON: bad escape '\\{e}'");
                }
            }
        }

        static object ParseNumber(string s, ref int pos)
        {
            int start = pos;
            bool isDouble = false;
            if (pos < s.Length && s[pos] == '-') pos++;
            while (pos < s.Length)
            {
                char c = s[pos];
                if (c >= '0' && c <= '9') { pos++; continue; }
                if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { isDouble = true; pos++; continue; }
                break;
            }
            string token = s.Substring(start, pos - start);
            if (token.Length == 0 || token == "-")
                throw new FormatException($"JSON: bad number at index {start}");
            if (!isDouble && long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                return l;
            return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static void Expect(string s, ref int pos, string literal)
        {
            if (pos + literal.Length > s.Length || string.CompareOrdinal(s, pos, literal, 0, literal.Length) != 0)
                throw new FormatException($"JSON: expected '{literal}' at index {pos}");
            pos += literal.Length;
        }

        static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length)
            {
                char c = s[pos];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') pos++;
                else break;
            }
        }

        // ---- writing ----

        static void WriteValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case string s: WriteString(sb, s); break;
                case sbyte or byte or short or ushort or int or uint or long:
                    sb.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                    break;
                case ulong ul: sb.Append(ul.ToString(CultureInfo.InvariantCulture)); break;
                case float f: WriteDouble(sb, f); break;
                case double d: WriteDouble(sb, d); break;
                case IDictionary<string, object> dict:
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(sb, kv.Key);
                        sb.Append(':');
                        WriteValue(sb, kv.Value);
                    }
                    sb.Append('}');
                    break;
                }
                case System.Collections.IEnumerable seq:
                {
                    sb.Append('[');
                    bool first = true;
                    foreach (var item in seq)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteValue(sb, item);
                    }
                    sb.Append(']');
                    break;
                }
                default:
                    throw new ArgumentException($"MiniJson cannot serialize {value.GetType().FullName}");
            }
        }

        static void WriteDouble(StringBuilder sb, double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
                throw new ArgumentException("MiniJson cannot serialize NaN/Infinity");
            sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
