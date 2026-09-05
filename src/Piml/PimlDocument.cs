using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Piml.Internal;

namespace Piml
{
    /// <summary>
    /// A PIML document that keeps its original text. Reading goes through <see cref="Root"/>;
    /// <see cref="Set(string[], PimlNode)"/> and <see cref="Remove"/> rewrite only the lines they touch, so comments,
    /// key order, blank lines, quoting and line endings survive a read → modify → write cycle.
    /// </summary>
    public sealed partial class PimlDocument
    {
        private readonly List<string> _lines;
        private readonly string _newLine;
        private readonly bool _trailingNewLine;
        private PimlObject _root = new PimlObject();
        private SpanNode _span = new SpanNode();

        private PimlDocument(List<string> lines, string newLine, bool trailingNewLine)
        {
            _lines = lines;
            _newLine = newLine;
            _trailingNewLine = trailingNewLine;
            Reparse();
        }

        /// <summary>Parses text into an editable document. Throws <see cref="PimlSyntaxException"/> on invalid input.</summary>
        public static PimlDocument Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var newLine = text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
            var raws = text.Split('\n');
            var lines = new List<string>(raws.Length);
            foreach (var raw in raws)
                lines.Add(raw.Length > 0 && raw[raw.Length - 1] == '\r' ? raw.Substring(0, raw.Length - 1) : raw);
            bool trailing = text.Length > 0 && text[text.Length - 1] == '\n';
            if (trailing) lines.RemoveAt(lines.Count - 1);
            if (text.Length == 0) lines.Clear();
            return new PimlDocument(lines, newLine, trailing);
        }

        /// <summary>The parsed value tree. Rebuilt after every edit; do not mutate it directly (changes are not written back).</summary>
        public PimlObject Root => _root;

        /// <summary>Line terminator detected on parse (<c>\r\n</c> if the text contained one, else <c>\n</c>).</summary>
        public string NewLine => _newLine;

        /// <summary>The current lines without terminators.</summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Navigates a path of object keys and array indices (digits). Returns null when any segment is missing.</summary>
        public PimlNode? Get(params string[] path)
        {
            PimlNode node = _root;
            foreach (var segment in path)
            {
                switch (node)
                {
                    case PimlObject obj:
                        if (!obj.TryGetValue(segment, out node)) return null;
                        break;
                    case PimlArray arr:
                        if (!TryIndex(segment, arr.Count, out var index)) return null;
                        node = arr[index];
                        break;
                    default:
                        return null;
                }
            }
            return node;
        }

        /// <summary>The document text with the original line endings and trailing newline.</summary>
        public override string ToString()
        {
            var text = string.Join(_newLine, _lines);
            return _trailingNewLine ? text + _newLine : text;
        }

        private void Reparse()
        {
            var scanned = new List<Line>(_lines.Count);
            for (int i = 0; i < _lines.Count; i++) scanned.Add(LineScanner.Classify(i, _lines[i]));
            _span = new SpanNode { Children = new Dictionary<string, SpanNode>(StringComparer.Ordinal) };
            _root = Parser.Parse(scanned, _span);
        }

        private static bool TryIndex(string segment, int count, out int index)
        {
            index = -1;
            if (segment.Length == 0) return false;
            foreach (var c in segment) if (c < '0' || c > '9') return false;
            if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index)) return false;
            return index >= 0 && index < count;
        }
    }

    public sealed partial class PimlDocument
    {
        private static readonly PimlWriterOptions RenderOptions = new PimlWriterOptions { NewLine = "\n", LabelObjectItems = false };

        /// <summary>Sets a root-level key.</summary>
        public void Set(string key, PimlNode? value) => Set(new[] { key }, value);

        /// <summary>
        /// Creates or replaces the node at <paramref name="path"/>. Single-line scalars are rewritten in place
        /// (their inline comment is kept); everything else replaces the node's line range. Missing parent
        /// objects are created; a <c>nil</c> parent becomes an object; an array index equal to the count appends.
        /// </summary>
        public void Set(string[] path, PimlNode? value)
        {
            if (path == null || path.Length == 0) throw new ArgumentException("Path must have at least one segment.", nameof(path));
            var node = value ?? PimlNull.Instance;
            var last = path[path.Length - 1];

            if (!TryResolve(path, path.Length - 1, out var parentSpan, out var parentNode))
            {
                var parentPath = new string[path.Length - 1];
                Array.Copy(path, parentPath, parentPath.Length);
                Set(parentPath, new PimlObject { { last, node } });
                return;
            }

            int childLevel = parentSpan.Indent / 2 + 1;   // root Indent is -2 → level 0

            if (parentNode is PimlArray array)
            {
                if (!TryIndexOrAppend(last, array.Count, out var index))
                    throw new PimlException("'" + last + "' is not a valid index for an array of " + array.Count + " items.");
                if (index < array.Count)
                {
                    var itemSpan = parentSpan.Items![index];
                    if (IsSingleLine(array[index]) && IsSingleLine(node)) RewriteScalarLine(itemSpan.HeadLine, node);
                    else ReplaceLines(itemSpan.HeadLine, itemSpan.EndLine, RenderItem(node, childLevel));
                }
                else
                {
                    InsertLines(parentSpan.EndLine + 1, RenderItem(node, childLevel));
                }
            }
            else if (parentNode is PimlObject obj && parentSpan.Children != null)
            {
                PimlObject.ValidateKey(last);
                if (parentSpan.Children.TryGetValue(last, out var existing))
                {
                    if (IsSingleLine(obj[last]) && IsSingleLine(node)) RewriteScalarLine(existing.HeadLine, node);
                    else ReplaceLines(existing.HeadLine, existing.EndLine, RenderKey(last, node, childLevel));
                }
                else
                {
                    int at = parentSpan.HeadLine < 0 ? _lines.Count : parentSpan.EndLine + 1;
                    InsertLines(at, RenderKey(last, node, childLevel));
                }
            }
            else
            {
                // The parent exists but is a scalar or nil: turn it into an object holding the new member.
                ReplaceLines(parentSpan.HeadLine, parentSpan.EndLine,
                    RenderHeadWithValue(parentSpan.HeadLine, new PimlObject { { last, node } }, parentSpan.Indent / 2));
            }

            Reparse();
        }

        /// <summary>Deletes the node at <paramref name="path"/> and its lines. Returns false if it does not exist.</summary>
        public bool Remove(params string[] path)
        {
            if (path == null || path.Length == 0) throw new ArgumentException("Path must have at least one segment.", nameof(path));
            if (!TryResolve(path, path.Length, out var span, out _)) return false;
            _lines.RemoveRange(span.HeadLine, span.EndLine - span.HeadLine + 1);
            Reparse();
            return true;
        }

        // ---- resolution ----

        private bool TryResolve(string[] path, int count, out SpanNode span, out PimlNode node)
        {
            span = _span;
            node = _root;
            for (int i = 0; i < count; i++)
            {
                var segment = path[i];
                switch (node)
                {
                    case PimlObject obj when span.Children != null && span.Children.TryGetValue(segment, out var childSpan):
                        span = childSpan;
                        node = obj[segment];
                        break;
                    case PimlArray arr when span.Items != null && TryIndex(segment, arr.Count, out var index):
                        span = span.Items[index];
                        node = arr[index];
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        private static bool TryIndexOrAppend(string segment, int count, out int index) =>
            TryIndex(segment, count + 1, out index);

        private static bool IsSingleLine(PimlNode node)
        {
            switch (node)
            {
                case PimlObject _: case PimlArray _: return false;
                case PimlString s: return s.Value.IndexOf('\n') < 0;
                default: return true;
            }
        }

        // ---- line editing ----

        private void InsertLines(int at, List<string> lines) => _lines.InsertRange(at, lines);

        private void ReplaceLines(int start, int end, List<string> lines)
        {
            _lines.RemoveRange(start, end - start + 1);
            _lines.InsertRange(start, lines);
        }

        /// <summary>Rewrites "(key) value # comment" or "> value # comment" keeping the head and the comment.</summary>
        private void RewriteScalarLine(int lineIndex, PimlNode value)
        {
            var raw = _lines[lineIndex];
            int indent = 0;
            while (indent < raw.Length && raw[indent] == ' ') indent++;
            string head;
            string rest;
            if (raw[indent] == '(')
            {
                int close = raw.IndexOf(')', indent);
                head = raw.Substring(0, close + 1);
                rest = raw.Substring(close + 1);
            }
            else
            {
                head = raw.Substring(0, indent + 1);   // "  >"
                rest = raw.Substring(indent + 1);
            }
            ScalarParser.Parse(rest, out var comment);
            var sb = new StringBuilder(head).Append(' ').Append(PimlWriter.FormatScalar(value));
            if (comment.Length > 0) sb.Append(' ').Append(comment);
            _lines[lineIndex] = sb.ToString();
        }

        private static List<string> RenderKey(string key, PimlNode value, int level)
        {
            var sb = new StringBuilder();
            sb.Append(' ', level * 2).Append('(').Append(key).Append(')');
            PimlWriter.AppendValue(sb, value, level, RenderOptions);
            return SplitRendered(sb);
        }

        private static List<string> RenderItem(PimlNode value, int level)
        {
            var sb = new StringBuilder();
            sb.Append(' ', level * 2).Append('>');
            PimlWriter.AppendItemValue(sb, value, level, RenderOptions);
            return SplitRendered(sb);
        }

        /// <summary>Re-renders an existing head line ("(key)" or "&gt;") with a new value, dropping any old inline value/comment.</summary>
        private List<string> RenderHeadWithValue(int headLine, PimlNode value, int level)
        {
            var raw = _lines[headLine];
            int indent = 0;
            while (indent < raw.Length && raw[indent] == ' ') indent++;
            if (raw[indent] == '(')
            {
                int close = raw.IndexOf(')', indent);
                return RenderKey(raw.Substring(indent + 1, close - indent - 1), value, level);
            }
            return RenderItem(value, level);
        }

        private static List<string> SplitRendered(StringBuilder sb)
        {
            var parts = sb.ToString().Split('\n');
            var lines = new List<string>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i == parts.Length - 1 && parts[i].Length == 0) break;   // trailing newline
                lines.Add(parts[i]);
            }
            return lines;
        }
    }
}
