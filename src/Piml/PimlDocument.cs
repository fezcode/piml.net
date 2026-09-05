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
}
