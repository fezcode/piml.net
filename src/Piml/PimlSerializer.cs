using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Piml.Internal;

namespace Piml
{
    /// <summary>Maps CLR objects to and from <see cref="PimlNode"/> trees by reflection.</summary>
    public static partial class PimlSerializer
    {
        internal const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK";

        /// <summary>Converts any CLR value to a node: scalars, enums (by name), dates (RFC 3339), dictionaries with string keys, sequences, and objects via their public members.</summary>
        public static PimlNode ToNode(object? value, PimlSerializerOptions? options = null)
        {
            var o = options ?? PimlSerializerOptions.Default;
            switch (value)
            {
                case null: return PimlNull.Instance;
                case PimlNode node: return node;
                case string s: return new PimlString(s);
                case bool b: return new PimlBoolean(b);
                case byte _: case sbyte _: case short _: case ushort _: case int _: case uint _: case long _:
                    return new PimlInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                case ulong ul:
                    return ul <= long.MaxValue ? (PimlNode)new PimlInteger((long)ul) : new PimlString(ul.ToString(CultureInfo.InvariantCulture));
                case float _: case double _:
                    return new PimlFloat(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                case decimal m: return new PimlFloat((double)m);
                case DateTime dt: return new PimlString(dt.ToString(DateFormat, CultureInfo.InvariantCulture));
                case DateTimeOffset dto: return new PimlString(dto.ToString(DateFormat, CultureInfo.InvariantCulture));
                case Guid g: return new PimlString(g.ToString("D"));
                case Uri u: return new PimlString(u.OriginalString);
                case Enum e: return new PimlString(e.ToString());
                case IDictionary dict:
                {
                    var obj = new PimlObject();
                    foreach (DictionaryEntry entry in dict)
                    {
                        if (!(entry.Key is string key))
                            throw new PimlException("Dictionary keys must be strings to serialize to PIML.");
                        obj.Add(key, ToNode(entry.Value, o));
                    }
                    return obj;
                }
                case IEnumerable seq:
                {
                    var arr = new PimlArray();
                    foreach (var item in seq) arr.Add(ToNode(item, o));
                    return arr;
                }
                default:
                    return ObjectToNode(value, o);
            }
        }

        private static PimlNode ObjectToNode(object value, PimlSerializerOptions o)
        {
            var obj = new PimlObject();
            foreach (var member in TypeInfoCache.Get(value.GetType()))
            {
                var v = member.Get(value);
                if ((member.OmitEmpty || o.OmitEmptyValues) && IsEmpty(v)) continue;
                obj.Add(member.KeyFor(o), ToNode(v, o));
            }
            return obj;
        }

        private static bool IsEmpty(object? v)
        {
            switch (v)
            {
                case null: return true;
                case string s: return s.Length == 0;
                case bool b: return !b;
                case byte _: case sbyte _: case short _: case ushort _: case int _: case uint _: case long _: case ulong _:
                    return Convert.ToDecimal(v, CultureInfo.InvariantCulture) == 0m;
                case float _: case double _: case decimal _:
                    return Convert.ToDouble(v, CultureInfo.InvariantCulture) == 0d;
                case PimlNull _: return true;
                case PimlObject po: return po.Count == 0;
                case PimlArray pa: return pa.Count == 0;
                case ICollection c: return c.Count == 0;
                case IEnumerable e: return !e.Cast<object?>().Any();
                default: return false;
            }
        }
    }
}
