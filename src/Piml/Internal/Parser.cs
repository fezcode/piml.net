using System.Collections.Generic;
using System.Text;

namespace Piml.Internal
{
    /// <summary>Recursive-descent parser over classified lines (spec 2, 3, 4).</summary>
    internal sealed class Parser
    {
        private readonly IReadOnlyList<Line> _lines;
        private int _pos;

        private Parser(IReadOnlyList<Line> lines)
        {
            _lines = lines;
        }

        public static PimlObject Parse(IReadOnlyList<Line> lines, SpanNode? rootSpan)
        {
            var parser = new Parser(lines);
            var root = new PimlObject();
            if (rootSpan != null && rootSpan.Children == null)
                rootSpan.Children = new Dictionary<string, SpanNode>(System.StringComparer.Ordinal);
            parser.ParseObjectBody(root, 0, rootSpan);
            int trailing = parser.NextSignificant(parser._pos);
            if (trailing >= 0) throw Err(lines[trailing], "Unexpected content after the document root.");
            return root;
        }

        // ---- helpers ----

        private int NextSignificant(int from)
        {
            for (int i = from; i < _lines.Count; i++)
            {
                var k = _lines[i].Kind;
                if (k != LineKind.Blank && k != LineKind.Comment) return i;
            }
            return -1;
        }

        private static PimlSyntaxException Err(Line line, string message) =>
            new PimlSyntaxException(message, line.Number, line.Indent + 1);

        private static string IndentMessage(Line line) =>
            line.Indent % 2 != 0
                ? "Indentation must be a multiple of 2 spaces."
                : "Unexpected indentation: children must be exactly one level (2 spaces) deeper than their parent.";

        // ---- objects ----

        private void ParseObjectBody(PimlObject obj, int level, SpanNode? span)
        {
            int expected = level * 2;
            while (true)
            {
                int i = NextSignificant(_pos);
                if (i < 0) { _pos = _lines.Count; return; }
                var line = _lines[i];
                if (line.Indent < expected) { _pos = i; return; }
                if (line.Indent > expected) throw Err(line, IndentMessage(line));

                switch (line.Kind)
                {
                    case LineKind.Key:
                        _pos = i + 1;
                        ParseKeyLine(obj, line, level, span);
                        break;
                    case LineKind.Item:
                        throw Err(line, level == 0
                            ? "Array items are not allowed at the document root; items need a key."
                            : "An array item is not allowed inside an object.");
                    default:
                        throw Err(line, "Expected a (key).");
                }
            }
        }

        private void ParseKeyLine(PimlObject obj, Line line, int level, SpanNode? parentSpan)
        {
            int close = line.Text.IndexOf(')');
            if (close < 0) throw Err(line, "Missing closing parenthesis for key.");
            var key = line.Text.Substring(1, close - 1);
            if (key.Length == 0) throw Err(line, "Key must not be empty.");
            if (key.IndexOf('(') >= 0) throw Err(line, "Key '" + key + "' must not contain parentheses.");
            if (obj.ContainsKey(key)) throw Err(line, "Duplicate key '" + key + "'.");
            var rest = line.Text.Substring(close + 1);

            SpanNode? span = null;
            if (parentSpan != null)
            {
                span = new SpanNode { HeadLine = line.Index, EndLine = line.Index, Indent = line.Indent };
                parentSpan.Children![key] = span;
            }

            int next = NextSignificant(_pos);
            bool hasBlock = next >= 0 && _lines[next].Indent > line.Indent;
            var trimmed = rest.Trim();
            bool hasInlineValue = trimmed.Length > 0 && trimmed[0] != '#';

            PimlNode node;
            if (hasInlineValue)
            {
                if (hasBlock)
                    throw Err(_lines[next], "Key '" + key + "' has an inline value and cannot also have an indented block.");
                node = ScalarParser.Parse(rest);
            }
            else if (!hasBlock)
            {
                node = PimlNull.Instance;
            }
            else
            {
                node = ParseBlock(line.Indent, level + 1, span);
            }

            obj.Add(key, node);
            if (parentSpan != null && span!.EndLine > parentSpan.EndLine) parentSpan.EndLine = span.EndLine;
        }

        // ---- blocks ----

        /// <summary>Parses the indented block under a bare key or item. The first significant line decides the type.</summary>
        private PimlNode ParseBlock(int parentIndent, int level, SpanNode? span)
        {
            int i = NextSignificant(_pos);
            var first = _lines[i];
            switch (first.Kind)
            {
                case LineKind.Key:
                {
                    var obj = new PimlObject();
                    if (span != null) span.Children = new Dictionary<string, SpanNode>(System.StringComparer.Ordinal);
                    ParseObjectBody(obj, level, span);
                    return obj;
                }
                case LineKind.Item:
                    throw Err(first, "Arrays are not supported yet."); // replaced in Task 6
                default:
                    throw Err(first, "Multi-line strings are not supported yet."); // replaced in Task 7
            }
        }
    }
}
