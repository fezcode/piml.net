using System.Globalization;
using System.Text.RegularExpressions;

namespace Piml.Internal
{
    internal static class ScalarParser
    {
        private static readonly Regex IntPattern = new Regex(@"^-?(0|[1-9][0-9]*)$", RegexOptions.Compiled);
        private static readonly Regex FloatPattern = new Regex(@"^-?(0|[1-9][0-9]*)\.[0-9]+$", RegexOptions.Compiled);

        public static PimlNode Parse(string rawValue) => Parse(rawValue, out _);

        /// <summary>
        /// Parses the text that follows a key's ')' or an item's '>'. Applies, in order:
        /// trimming, the quoted-value rule (spec 4.1.2), inline-comment stripping (spec 3.3),
        /// the \# escape (spec 3.4) and type inference (spec 4.1).
        /// </summary>
        public static PimlNode Parse(string rawValue, out string comment)
        {
            comment = "";
            var text = rawValue.Trim();
            if (text.Length == 0) return PimlNull.Instance;

            if (text[0] == '#')
            {
                comment = text;
                return PimlNull.Instance;
            }

            if (text[0] == '"')
            {
                int last = text.LastIndexOf('"');
                if (last > 0)
                {
                    var tail = text.Substring(last + 1);
                    if (IsEmptyOrComment(tail))
                    {
                        comment = tail.Trim();
                        return new PimlString(text.Substring(1, last - 1));
                    }
                }
            }

            int hash = FindInlineComment(text);
            if (hash >= 0)
            {
                comment = text.Substring(hash).Trim();
                text = text.Substring(0, hash).TrimEnd();
            }

            text = text.Replace("\\#", "#");
            return Infer(text);
        }

        /// <summary>Index of the first '#' preceded by a space or tab, or -1.</summary>
        public static int FindInlineComment(string text)
        {
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == '#' && (text[i - 1] == ' ' || text[i - 1] == '\t')) return i;
            }
            return -1;
        }

        /// <summary>Schemaless type inference for an already-unquoted, comment-free value.</summary>
        public static PimlNode Infer(string text)
        {
            if (text == "nil") return PimlNull.Instance;
            if (text == "true") return new PimlBoolean(true);
            if (text == "false") return new PimlBoolean(false);
            if (IntPattern.IsMatch(text)
                && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l))
                return new PimlInteger(l);
            if (FloatPattern.IsMatch(text)
                && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return new PimlFloat(d);
            return new PimlString(text);
        }

        private static bool IsEmptyOrComment(string tail)
        {
            if (tail.Trim().Length == 0) return true;
            if (!char.IsWhiteSpace(tail[0])) return false;
            var rest = tail.TrimStart();
            return rest.Length > 0 && rest[0] == '#';
        }
    }
}
