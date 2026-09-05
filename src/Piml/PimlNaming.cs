using System.Text;

namespace Piml
{
    /// <summary>Built-in key naming policies for <see cref="PimlSerializerOptions.KeyNamingPolicy"/>.</summary>
    public static class PimlNaming
    {
        /// <summary>Member names unchanged.</summary>
        public static string AsIs(string name) => name;

        /// <summary><c>SiteName</c> → <c>siteName</c>, <c>HTTPServer</c> → <c>httpServer</c>, <c>URL</c> → <c>url</c>.</summary>
        public static string CamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var words = SplitWords(name);
            var sb = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                var w = words[i].ToLowerInvariant();
                if (i == 0) sb.Append(w);
                else sb.Append(char.ToUpperInvariant(w[0])).Append(w.Substring(1));
            }
            return sb.ToString();
        }

        /// <summary><c>SiteName</c> → <c>site_name</c>, <c>HTTPServer</c> → <c>http_server</c>.</summary>
        public static string SnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var words = SplitWords(name);
            var sb = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                if (i > 0) sb.Append('_');
                sb.Append(words[i].ToLowerInvariant());
            }
            return sb.ToString();
        }

        /// <summary>Splits on case boundaries: "HTTPServer" → ["HTTP", "Server"], "siteName" → ["site", "Name"].</summary>
        private static System.Collections.Generic.List<string> SplitWords(string name)
        {
            var words = new System.Collections.Generic.List<string>();
            int start = 0;
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '_')
                {
                    if (i > start) words.Add(name.Substring(start, i - start));
                    start = i + 1;
                    continue;
                }
                if (i == start) continue;
                char prev = name[i - 1], cur = name[i];
                bool boundary =
                    (char.IsUpper(cur) && !char.IsUpper(prev)) ||
                    (char.IsUpper(cur) && char.IsUpper(prev) && i + 1 < name.Length && char.IsLower(name[i + 1]));
                if (boundary) { words.Add(name.Substring(start, i - start)); start = i; }
            }
            if (start < name.Length) words.Add(name.Substring(start));
            return words;
        }
    }
}
