using System.Collections.Generic;

namespace Piml.Internal
{
    internal static class LineScanner
    {
        /// <summary>Splits text into classified lines. LF and CRLF are both accepted.</summary>
        public static List<Line> Scan(string text)
        {
            var raws = text.Split('\n');
            var lines = new List<Line>(raws.Length);
            for (int i = 0; i < raws.Length; i++)
            {
                var raw = raws[i];
                if (raw.Length > 0 && raw[raw.Length - 1] == '\r') raw = raw.Substring(0, raw.Length - 1);
                lines.Add(Classify(i, raw));
            }
            return lines;
        }

        /// <summary>Classifies one line (already stripped of CR/LF).</summary>
        public static Line Classify(int index, string raw)
        {
            int i = 0;
            while (i < raw.Length && raw[i] == ' ') i++;

            int j = i;
            while (j < raw.Length && char.IsWhiteSpace(raw[j])) j++;
            if (j >= raw.Length) return new Line(index, raw, i, LineKind.Blank, "");

            if (raw[i] == '\t')
                throw new PimlSyntaxException("Tabs are not allowed in indentation.", index + 1, i + 1);

            char c = raw[i];
            LineKind kind =
                c == '#' ? LineKind.Comment :
                c == '(' ? LineKind.Key :
                c == '>' ? LineKind.Item :
                LineKind.Content;
            return new Line(index, raw, i, kind, raw.Substring(i));
        }
    }
}
