using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Substation.Shared
{
    /// <summary>
    /// Minimal JSON helpers built on JavaScriptSerializer, which ships with the
    /// .NET Framework (System.Web.Extensions). The config files and the stored
    /// hours are read and written without pulling in a JSON NuGet package —
    /// the substation PCs are offline, so every extra restore is a liability.
    /// </summary>
    public static class Json
    {
        private static JavaScriptSerializer NewSerializer()
        {
            return new JavaScriptSerializer
            {
                MaxJsonLength = 32 * 1024 * 1024,
                RecursionLimit = 64
            };
        }

        public static string Write(object value)
        {
            return NewSerializer().Serialize(value);
        }

        public static Dictionary<string, object> Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, object>();
            var obj = NewSerializer().DeserializeObject(text) as Dictionary<string, object>;
            return obj ?? new Dictionary<string, object>();
        }

        public static Dictionary<string, object> ReadFile(string path)
        {
            return Parse(File.ReadAllText(path, Encoding.UTF8));
        }

        /// <summary>Writes indented JSON so operators can hand-edit config files.</summary>
        public static void WriteFile(string path, object value)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Write through a temp file so a crash mid-write cannot leave the
            // agent with a truncated, unparsable configuration.
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, Pretty(Write(value)), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        // ── Typed accessors ────────────────────────────────────────────────
        // JavaScriptSerializer hands back object graphs of Dictionary/ArrayList/
        // primitives, so every read goes through a tolerant converter: a config
        // typo should degrade to a default, never throw during hourly capture.

        public static Dictionary<string, object> Dict(object node)
        {
            return node as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        public static Dictionary<string, object> Dict(Dictionary<string, object> src, string key)
        {
            object v;
            return (src != null && src.TryGetValue(key, out v)) ? Dict(v) : new Dictionary<string, object>();
        }

        public static List<object> List(Dictionary<string, object> src, string key)
        {
            object v;
            if (src == null || !src.TryGetValue(key, out v)) return new List<object>();
            var arr = v as ArrayList;
            if (arr != null) return new List<object>(arr.ToArray());
            var objs = v as object[];
            if (objs != null) return new List<object>(objs);
            var list = v as List<object>;
            return list ?? new List<object>();
        }

        public static string Str(Dictionary<string, object> src, string key, string fallback = "")
        {
            object v;
            if (src == null || !src.TryGetValue(key, out v) || v == null) return fallback;
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public static int Int(Dictionary<string, object> src, string key, int fallback)
        {
            object v;
            if (src == null || !src.TryGetValue(key, out v) || v == null) return fallback;
            int parsed;
            if (int.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                             NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) return parsed;
            double d;
            if (double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                                NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return (int)Math.Round(d);
            return fallback;
        }

        public static double Dbl(Dictionary<string, object> src, string key, double fallback)
        {
            object v;
            if (src == null || !src.TryGetValue(key, out v) || v == null) return fallback;
            double d;
            return double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                                   NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : fallback;
        }

        public static bool Bool(Dictionary<string, object> src, string key, bool fallback)
        {
            object v;
            if (src == null || !src.TryGetValue(key, out v) || v == null) return fallback;
            if (v is bool) return (bool)v;
            string s = Convert.ToString(v, CultureInfo.InvariantCulture).Trim();
            if (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1") return true;
            if (s.Equals("false", StringComparison.OrdinalIgnoreCase) || s == "0") return false;
            return fallback;
        }

        public static List<string> Strings(Dictionary<string, object> src, string key)
        {
            var result = new List<string>();
            foreach (object o in List(src, key))
                result.Add(Convert.ToString(o, CultureInfo.InvariantCulture));
            return result;
        }

        // ── Pretty printer ─────────────────────────────────────────────────
        // JavaScriptSerializer only emits compact JSON. Config files are edited
        // by substation engineers in Notepad, so indent them on the way out.
        public static string Pretty(string compact)
        {
            if (string.IsNullOrEmpty(compact)) return compact;

            var sb = new StringBuilder(compact.Length * 2);
            int depth = 0;
            bool inString = false, escaped = false;

            foreach (char c in compact)
            {
                if (inString)
                {
                    sb.Append(c);
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        sb.Append(c);
                        break;
                    case '{':
                    case '[':
                        sb.Append(c);
                        depth++;
                        NewLine(sb, depth);
                        break;
                    case '}':
                    case ']':
                        depth--;
                        NewLine(sb, depth);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        NewLine(sb, depth);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static void NewLine(StringBuilder sb, int depth)
        {
            sb.Append(Environment.NewLine);
            sb.Append(' ', depth * 2);
        }
    }
}
