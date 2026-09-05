namespace Piml.Internal
{
    internal enum LineKind
    {
        Blank,
        Comment,
        Key,
        Item,
        Content
    }

    internal sealed class Line
    {
        public Line(int index, string raw, int indent, LineKind kind, string text)
        {
            Index = index;
            Raw = raw;
            Indent = indent;
            Kind = kind;
            Text = text;
        }

        /// <summary>0-based line index.</summary>
        public int Index { get; }
        /// <summary>1-based line number for messages.</summary>
        public int Number => Index + 1;
        /// <summary>The line without its trailing CR/LF.</summary>
        public string Raw { get; }
        /// <summary>Number of leading spaces.</summary>
        public int Indent { get; }
        public LineKind Kind { get; }
        /// <summary>Raw text after the indentation; empty for blank lines.</summary>
        public string Text { get; }
    }
}
