using System.Collections;
using System.Collections.Generic;

namespace Piml
{
    /// <summary>An ordered list of nodes.</summary>
    public sealed class PimlArray : PimlNode, IList<PimlNode>
    {
        private readonly List<PimlNode> _items = new List<PimlNode>();

        /// <inheritdoc/>
        public override PimlNodeKind Kind => PimlNodeKind.Array;
        /// <inheritdoc/>
        public int Count => _items.Count;
        /// <inheritdoc/>
        public bool IsReadOnly => false;
        /// <inheritdoc/>
        public PimlNode this[int index]
        {
            get => _items[index];
            set => _items[index] = value ?? PimlNull.Instance;
        }
        /// <inheritdoc/>
        public void Add(PimlNode? item) => _items.Add(item ?? PimlNull.Instance);
        /// <inheritdoc/>
        public void Clear() => _items.Clear();
        /// <inheritdoc/>
        public bool Contains(PimlNode item) => _items.Contains(item);
        /// <inheritdoc/>
        public void CopyTo(PimlNode[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        /// <inheritdoc/>
        public int IndexOf(PimlNode item) => _items.IndexOf(item);
        /// <inheritdoc/>
        public void Insert(int index, PimlNode? item) => _items.Insert(index, item ?? PimlNull.Instance);
        /// <inheritdoc/>
        public bool Remove(PimlNode item) => _items.Remove(item);
        /// <inheritdoc/>
        public void RemoveAt(int index) => _items.RemoveAt(index);
        /// <inheritdoc/>
        public IEnumerator<PimlNode> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
