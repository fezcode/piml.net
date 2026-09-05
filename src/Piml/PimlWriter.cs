using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Piml
{
    /// <summary>Writes a node tree as canonical PIML text that parses back to the same tree.</summary>
    public static class PimlWriter
    {
        private static readonly Regex IntPattern = new Regex(@"^-?(0|[1-9][0-9]*)$", RegexOptions.Compiled);
        private static readonly Regex FloatPattern = new Regex(@"^-?(0|[1-9][0-9]*)\.[0-9]+$", RegexOptions.Compiled);

        /// <summary>Writes the root object. Empty containers and nulls become <c>nil</c>.</summary>
        public static string Write(PimlObject root, PimlWriterOptions? options = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var o = options ?? PimlWriterOptions.Default;
            var sb = new StringBuilder();
            AppendObjectBody(sb, root, 0, o);
            return sb.ToString();
        }

        /// <summary>Formats a scalar node as it appears after a key: <c>nil</c>, <c>true</c>, numbers, or a (quoted if needed) string.</summary>
        public static string FormatScalar(PimlNode node)
        {
            switch (node)
            {
                case PimlNull _: return "nil";
                case PimlBoolean b: return b.Value ? "true" : "false";
                case PimlInteger i: return i.Value.ToString(CultureInfo.InvariantCulture);
                case PimlFloat f: return FormatFloat(f.Value);
                case PimlString s:
                    if (s.Value.IndexOf('\n') >= 0)
                        throw new PimlException("A multi-line string cannot be formatted as a single-line scalar.");
                    return NeedsQuoting(s.Value) ? "\"" + s.Value + "\"" : s.Value;
                default:
                    throw new PimlException("Node kind " + node.Kind + " is not a scalar.");
            }
        }

        /// <summary>True when a single-line string must be quoted to parse back unchanged (mirrors go-piml).</summary>
        public static bool NeedsQuoting(string s)
        {
            if (s.Length == 0) return true;
            if (s != s.Trim()) return true;
            if (s == "nil" || s == "true" || s == "false") return true;
            if (IntPattern.IsMatch(s) || FloatPattern.IsMatch(s)) return true;
            if (s[0] == '"' || s[0] == '#') return true;
            if (s.IndexOf("\\#", StringComparison.Ordinal) >= 0) return true;
            for (int i = 1; i < s.Length; i++)
                if (s[i] == '#' && (s[i - 1] == ' ' || s[i - 1] == '\t')) return true;
            return false;
        }

        private static string FormatFloat(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
                throw new PimlException("NaN and Infinity cannot be represented in PIML.");
            var s = d.ToString("R", CultureInfo.InvariantCulture);
            if (s.IndexOf('E') >= 0 || s.IndexOf('e') >= 0)
                s = d.ToString("0.0###################", CultureInfo.InvariantCulture);
            if (s.IndexOf('.') < 0) s += ".0";
            return s;
        }

        private static string Indent(int level) => level <= 0 ? "" : new string(' ', level * 2);

        private static void AppendObjectBody(StringBuilder sb, PimlObject obj, int level, PimlWriterOptions o)
        {
            foreach (var kv in obj)
            {
                PimlObject.ValidateKey(kv.Key);
                sb.Append(Indent(level)).Append('(').Append(kv.Key).Append(')');
                AppendValue(sb, kv.Value, level, o);
            }
        }

        /// <summary>Appends what follows a key at <paramref name="level"/>: " scalar" + newline, or newline + an indented block.</summary>
        internal static void AppendValue(StringBuilder sb, PimlNode node, int level, PimlWriterOptions o)
        {
            switch (node)
            {
                case PimlObject obj when obj.Count == 0:
                case PimlArray arr0 when arr0.Count == 0:
                    sb.Append(" nil").Append(o.NewLine);
                    break;
                case PimlObject obj:
                    sb.Append(o.NewLine);
                    AppendObjectBody(sb, obj, level + 1, o);
                    break;
                case PimlArray arr:
                    sb.Append(o.NewLine);
                    AppendArrayItems(sb, arr, level + 1, o);
                    break;
                case PimlString s when s.Value.IndexOf('\n') >= 0:
                    sb.Append(o.NewLine);
                    AppendMultiline(sb, s.Value, level + 1, o);
                    break;
                default:
                    sb.Append(' ').Append(FormatScalar(node)).Append(o.NewLine);
                    break;
            }
        }

        private static void AppendArrayItems(StringBuilder sb, PimlArray arr, int level, PimlWriterOptions o)
        {
            foreach (var item in arr)
            {
                sb.Append(Indent(level)).Append('>');
                AppendItemValue(sb, item, level, o);
            }
        }

        /// <summary>Appends what follows an item's '&gt;' at <paramref name="level"/>.</summary>
        internal static void AppendItemValue(StringBuilder sb, PimlNode item, int level, PimlWriterOptions o)
        {
            switch (item)
            {
                case PimlObject obj when obj.Count == 0:
                case PimlArray arr0 when arr0.Count == 0:
                    sb.Append(" nil").Append(o.NewLine);
                    break;
                case PimlObject obj:
                    if (o.LabelObjectItems) sb.Append(" (").Append(o.ItemLabel).Append(')');
                    sb.Append(o.NewLine);
                    AppendObjectBody(sb, obj, level + 1, o);
                    break;
                case PimlArray arr:
                    sb.Append(o.NewLine);
                    AppendArrayItems(sb, arr, level + 1, o);
                    break;
                case PimlString s when s.Value.IndexOf('\n') >= 0:
                    sb.Append(o.NewLine);
                    AppendMultiline(sb, s.Value, level + 1, o);
                    break;
                default:
                    sb.Append(' ').Append(FormatScalar(item)).Append(o.NewLine);
                    break;
            }
        }

        private static void AppendMultiline(StringBuilder sb, string value, int level, PimlWriterOptions o)
        {
            var lines = value.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length > 0 && line[line.Length - 1] == '\r') line = line.Substring(0, line.Length - 1);
                if (line.Trim().Length == 0) { sb.Append(o.NewLine); continue; }
                sb.Append(Indent(level)).Append(EscapeContentLine(line, i == 0)).Append(o.NewLine);
            }
        }

        /// <summary>Adds the positional escapes of spec 3.4: a leading '#' always; a leading '(' or '&gt;' on the first line.</summary>
        private static string EscapeContentLine(string line, bool firstLine)
        {
            int i = 0;
            while (i < line.Length && line[i] == ' ') i++;
            if (i < line.Length)
            {
                char c = line[i];
                if (c == '#' || (firstLine && (c == '(' || c == '>')))
                    return line.Substring(0, i) + "\\" + line.Substring(i);
            }
            return line;
        }
    }
}
