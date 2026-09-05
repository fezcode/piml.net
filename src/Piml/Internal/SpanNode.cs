using System.Collections.Generic;

namespace Piml.Internal
{
    /// <summary>Line range of a parsed node (0-based, inclusive). Built only when a document needs edits.</summary>
    internal sealed class SpanNode
    {
        /// <summary>Index of the key/item line; -1 for the root.</summary>
        public int HeadLine = -1;
        /// <summary>Index of the last line that belongs to the node (== HeadLine for single-line scalars).</summary>
        public int EndLine = -1;
        /// <summary>Indent (spaces) of the head line; -2 for the root so children compute to level 0.</summary>
        public int Indent = -2;
        public Dictionary<string, SpanNode>? Children;
        public List<SpanNode>? Items;
    }
}
