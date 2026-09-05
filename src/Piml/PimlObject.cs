using System;
using System.Collections;
using System.Collections.Generic;

namespace Piml
{
    /// <summary>An ordered map of string keys to nodes. Keys must be non-empty and contain no parentheses.</summary>
    public sealed class PimlObject : PimlNode, IEnumerable<KeyValuePair<string, PimlNode>>
    {
        private readonly List<string> _keys = new List<string>();
        private readonly Dictionary<string, PimlNode> _map = new Dictionary<string, PimlNode>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public override PimlNodeKind Kind => PimlNodeKind.Object;

        /// <summary>Number of members.</summary>
        public int Count => _keys.Count;

        /// <summary>Keys in insertion order.</summary>
        public IReadOnlyList<string> Keys => _keys;

        /// <summary>Gets a member (throws <see cref="KeyNotFoundException"/>) or sets it via <see cref="Set"/>.</summary>
        public PimlNode this[string key]
        {
            get => _map[key];
            set => Set(key, value);
        }

        /// <summary>True if the key exists.</summary>
        public bool ContainsKey(string key) => _map.ContainsKey(key);

        /// <summary>Tries to get a member.</summary>
        public bool TryGetValue(string key, out PimlNode value) => _map.TryGetValue(key, out value!);

        /// <summary>Adds a member; throws <see cref="ArgumentException"/> if the key already exists or is invalid.</summary>
        public void Add(string key, PimlNode? value)
        {
            ValidateKey(key);
            if (_map.ContainsKey(key)) throw new ArgumentException("Duplicate key '" + key + "'.", nameof(key));
            _keys.Add(key);
            _map[key] = value ?? PimlNull.Instance;
        }

        /// <summary>Adds or replaces a member. A replaced member keeps its position.</summary>
        public void Set(string key, PimlNode? value)
        {
            ValidateKey(key);
            if (!_map.ContainsKey(key)) _keys.Add(key);
            _map[key] = value ?? PimlNull.Instance;
        }

        /// <summary>Removes a member; returns false if it did not exist.</summary>
        public bool Remove(string key)
        {
            if (!_map.Remove(key)) return false;
            _keys.Remove(key);
            return true;
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, PimlNode>> GetEnumerator()
        {
            foreach (var key in _keys) yield return new KeyValuePair<string, PimlNode>(key, _map[key]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key must not be empty.", nameof(key));
            if (key.IndexOf('(') >= 0 || key.IndexOf(')') >= 0)
                throw new ArgumentException("Key '" + key + "' must not contain parentheses.", nameof(key));
        }
    }
}
