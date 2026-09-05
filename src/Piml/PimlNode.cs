using System;
using System.Collections.Generic;
using System.Globalization;

namespace Piml
{
    /// <summary>The kind of value a <see cref="PimlNode"/> holds.</summary>
    public enum PimlNodeKind
    {
        /// <summary><c>nil</c>: null, empty array or empty object.</summary>
        Null,
        /// <summary><c>true</c> / <c>false</c>.</summary>
        Boolean,
        /// <summary>A 64-bit signed integer.</summary>
        Integer,
        /// <summary>A double-precision float.</summary>
        Float,
        /// <summary>A string (single- or multi-line).</summary>
        String,
        /// <summary>An ordered list of nodes.</summary>
        Array,
        /// <summary>An ordered map of keys to nodes.</summary>
        Object
    }

    /// <summary>Base class of the PIML value tree.</summary>
    public abstract class PimlNode
    {
        /// <summary>The kind of this node.</summary>
        public abstract PimlNodeKind Kind { get; }

        /// <summary>The shared <c>nil</c> node.</summary>
        public static PimlNull Null => PimlNull.Instance;

        /// <summary>Wraps a string; a null string becomes <see cref="PimlNull"/>.</summary>
        public static implicit operator PimlNode(string? value) =>
            value == null ? (PimlNode)PimlNull.Instance : new PimlString(value);

        /// <summary>Wraps a 64-bit integer.</summary>
        public static implicit operator PimlNode(long value) => new PimlInteger(value);

        /// <summary>Wraps a 32-bit integer.</summary>
        public static implicit operator PimlNode(int value) => new PimlInteger(value);

        /// <summary>Wraps a double.</summary>
        public static implicit operator PimlNode(double value) => new PimlFloat(value);

        /// <summary>Wraps a boolean.</summary>
        public static implicit operator PimlNode(bool value) => new PimlBoolean(value);

        /// <summary>
        /// Structural equality: scalars by kind and value, arrays by ordered elements,
        /// objects by key set and values (key order is ignored).
        /// </summary>
        public static bool DeepEquals(PimlNode? a, PimlNode? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Kind != b.Kind) return false;
            switch (a)
            {
                case PimlNull _: return true;
                case PimlBoolean x: return x.Value == ((PimlBoolean)b).Value;
                case PimlInteger x: return x.Value == ((PimlInteger)b).Value;
                case PimlFloat x: return x.Value.Equals(((PimlFloat)b).Value);
                case PimlString x: return string.Equals(x.Value, ((PimlString)b).Value, StringComparison.Ordinal);
                case PimlArray x:
                {
                    var y = (PimlArray)b;
                    if (x.Count != y.Count) return false;
                    for (int i = 0; i < x.Count; i++)
                        if (!DeepEquals(x[i], y[i])) return false;
                    return true;
                }
                case PimlObject x:
                {
                    var y = (PimlObject)b;
                    if (x.Count != y.Count) return false;
                    foreach (var kv in x)
                    {
                        if (!y.TryGetValue(kv.Key, out var other)) return false;
                        if (!DeepEquals(kv.Value, other)) return false;
                    }
                    return true;
                }
                default: return false;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PimlNode other && DeepEquals(this, other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int h = (int)Kind * 397;
                switch (this)
                {
                    case PimlBoolean x: return h ^ (x.Value ? 1 : 0);
                    case PimlInteger x: return h ^ x.Value.GetHashCode();
                    case PimlFloat x: return h ^ x.Value.GetHashCode();
                    case PimlString x: return h ^ StringComparer.Ordinal.GetHashCode(x.Value);
                    case PimlArray x:
                        foreach (var item in x) h = h * 31 + item.GetHashCode();
                        return h;
                    case PimlObject x:
                    {
                        int sum = 0; // order-independent
                        foreach (var kv in x) sum += StringComparer.Ordinal.GetHashCode(kv.Key) ^ kv.Value.GetHashCode();
                        return h ^ sum;
                    }
                    default: return h;
                }
            }
        }
    }

    /// <summary>The <c>nil</c> node (null, empty array, or empty object).</summary>
    public sealed class PimlNull : PimlNode
    {
        /// <summary>The single shared instance.</summary>
        public static readonly PimlNull Instance = new PimlNull();
        private PimlNull() { }
        /// <inheritdoc/>
        public override PimlNodeKind Kind => PimlNodeKind.Null;
        /// <inheritdoc/>
        public override string ToString() => "nil";
    }

    /// <summary>A boolean node.</summary>
    public sealed class PimlBoolean : PimlNode
    {
        /// <summary>Creates a boolean node.</summary>
        public PimlBoolean(bool value) { Value = value; }
        /// <summary>The value.</summary>
        public bool Value { get; }
        /// <inheritdoc/>
        public override PimlNodeKind Kind => PimlNodeKind.Boolean;
        /// <inheritdoc/>
        public override string ToString() => Value ? "true" : "false";
    }

    /// <summary>An integer node (64-bit).</summary>
    public sealed class PimlInteger : PimlNode
    {
        /// <summary>Creates an integer node.</summary>
        public PimlInteger(long value) { Value = value; }
        /// <summary>The value.</summary>
        public long Value { get; }
        /// <inheritdoc/>
        public override PimlNodeKind Kind => PimlNodeKind.Integer;
        /// <inheritdoc/>
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A floating-point node (double).</summary>
    public sealed class PimlFloat : PimlNode
    {
        /// <summary>Creates a float node.</summary>
        public PimlFloat(double value) { Value = value; }
        /// <summary>The value.</summary>
        public double Value { get; }
        /// <inheritdoc/>
        public override PimlNodeKind Kind => PimlNodeKind.Float;
        /// <inheritdoc/>
        public override string ToString() => Value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>A string node. Multi-line values contain <c>\n</c>.</summary>
    public sealed class PimlString : PimlNode
    {
        /// <summary>Creates a string node.</summary>
        public PimlString(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Value = value;
        }
        /// <summary>The value.</summary>
        public string Value { get; }
        /// <inheritdoc/>
        public override PimlNodeKind Kind => PimlNodeKind.String;
        /// <inheritdoc/>
        public override string ToString() => Value;
    }
}
