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

    public static partial class PimlSerializer
    {
        /// <summary>Binds a node to <typeparamref name="T"/>.</summary>
        public static T? FromNode<T>(PimlNode node, PimlSerializerOptions? options = null) =>
            (T?)FromNode(node, typeof(T), options);

        /// <summary>Binds a node to <paramref name="type"/> using the rules described on <see cref="Piml.Deserialize{T}"/>.</summary>
        public static object? FromNode(PimlNode node, Type type, PimlSerializerOptions? options = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Bind(node, type, options ?? PimlSerializerOptions.Default);
        }

        /// <summary>Converts a node to plain CLR values: null, bool, long, double, string, <c>List&lt;object?&gt;</c>, <c>Dictionary&lt;string, object?&gt;</c>.</summary>
        public static object? ToClr(PimlNode node)
        {
            switch (node)
            {
                case PimlNull _: return null;
                case PimlBoolean b: return b.Value;
                case PimlInteger i: return i.Value;
                case PimlFloat f: return f.Value;
                case PimlString s: return s.Value;
                case PimlArray a:
                {
                    var list = new List<object?>(a.Count);
                    foreach (var item in a) list.Add(ToClr(item));
                    return list;
                }
                case PimlObject o:
                {
                    var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var kv in o) dict[kv.Key] = ToClr(kv.Value);
                    return dict;
                }
                default: throw new PimlException("Unknown node kind " + node.Kind + ".");
            }
        }

        private static PimlException Mismatch(PimlNode node, Type type) =>
            new PimlException("Cannot convert PIML " + node.Kind + " to " + type.FullName + ".");

        private static object? Bind(PimlNode node, Type type, PimlSerializerOptions o)
        {
            if (typeof(PimlNode).IsAssignableFrom(type))
            {
                if (type.IsInstanceOfType(node)) return node;
                if (node is PimlNull) return null;
                throw Mismatch(node, type);
            }
            if (type == typeof(object)) return ToClr(node);

            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null) return node is PimlNull ? null : Bind(node, underlying, o);

            if (type == typeof(string))
            {
                switch (node)
                {
                    case PimlNull _: return null;
                    case PimlString s: return s.Value;
                    case PimlBoolean _: case PimlInteger _: case PimlFloat _: return PimlWriter.FormatScalar(node);
                    default: throw Mismatch(node, type);
                }
            }

            if (node is PimlNull)
            {
                if (IsDictionaryTarget(type, out _)) node = new PimlObject();
                else if (IsCollectionTarget(type, out _)) node = new PimlArray();
                else return type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            if (type == typeof(bool))
            {
                if (node is PimlBoolean b) return b.Value;
                if (node is PimlString bs && bs.Value == "true") return true;
                if (node is PimlString bs2 && bs2.Value == "false") return false;
                throw Mismatch(node, type);
            }
            if (IsIntegral(type))
            {
                long v;
                if (node is PimlInteger i) v = i.Value;
                else if (node is PimlString s && long.TryParse(s.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed)) v = parsed;
                else throw Mismatch(node, type);
                try { return Convert.ChangeType(v, type, CultureInfo.InvariantCulture); }
                catch (OverflowException ex) { throw new PimlException("Value " + v + " does not fit " + type.Name + ".", ex); }
            }
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                double v;
                if (node is PimlFloat f) v = f.Value;
                else if (node is PimlInteger i) v = i.Value;
                else if (node is PimlString s && double.TryParse(s.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) v = parsed;
                else throw Mismatch(node, type);
                return Convert.ChangeType(v, type, CultureInfo.InvariantCulture);
            }
            if (type.IsEnum)
            {
                if (node is PimlString es)
                {
                    try { return Enum.Parse(type, es.Value, true); }
                    catch (ArgumentException ex) { throw new PimlException("'" + es.Value + "' is not a value of " + type.Name + ".", ex); }
                }
                if (node is PimlInteger ei) return Enum.ToObject(type, ei.Value);
                throw Mismatch(node, type);
            }
            if (type == typeof(DateTime)) return DateTime.Parse(StringOf(node, type), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(StringOf(node, type), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (type == typeof(Guid)) return Guid.Parse(StringOf(node, type));
            if (type == typeof(Uri)) return new Uri(StringOf(node, type), UriKind.RelativeOrAbsolute);

            if (IsDictionaryTarget(type, out var valueType))
            {
                if (!(node is PimlObject dobj)) throw Mismatch(node, type);
                return BindDictionary(dobj, type, valueType!, o);
            }
            if (IsCollectionTarget(type, out var elementType))
            {
                if (!(node is PimlArray arr)) throw Mismatch(node, type);
                return BindCollection(arr, type, elementType!, o);
            }
            if (node is PimlObject obj) return BindObject(obj, type, o);
            throw Mismatch(node, type);
        }

        private static string StringOf(PimlNode node, Type type) =>
            node is PimlString s ? s.Value : throw Mismatch(node, type);

        private static bool IsIntegral(Type t) =>
            t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
            t == typeof(sbyte) || t == typeof(uint) || t == typeof(ushort) || t == typeof(ulong);

        /// <summary>Dictionary&lt;string,T&gt;, IDictionary&lt;string,T&gt;, IReadOnlyDictionary&lt;string,T&gt;, or a concrete IDictionary&lt;string,T&gt; with a parameterless constructor.</summary>
        private static bool IsDictionaryTarget(Type type, out Type? valueType)
        {
            valueType = null;
            var candidates = new List<Type> { type };
            candidates.AddRange(type.GetInterfaces());
            foreach (var c in candidates)
            {
                if (!c.IsGenericType) continue;
                var def = c.GetGenericTypeDefinition();
                if ((def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>)) && c.GetGenericArguments()[0] == typeof(string))
                {
                    valueType = c.GetGenericArguments()[1];
                    return true;
                }
            }
            return false;
        }

        /// <summary>T[], List&lt;T&gt;, IList/ICollection/IEnumerable/IReadOnlyList/IReadOnlyCollection&lt;T&gt;, or a concrete ICollection&lt;T&gt; with a parameterless constructor.</summary>
        private static bool IsCollectionTarget(Type type, out Type? elementType)
        {
            elementType = null;
            if (type == typeof(string)) return false;
            if (type.IsArray) { elementType = type.GetElementType(); return true; }
            var candidates = new List<Type> { type };
            candidates.AddRange(type.GetInterfaces());
            foreach (var c in candidates)
            {
                if (!c.IsGenericType) continue;
                var def = c.GetGenericTypeDefinition();
                if (def == typeof(IEnumerable<>) || def == typeof(ICollection<>) || def == typeof(IList<>) ||
                    def == typeof(IReadOnlyCollection<>) || def == typeof(IReadOnlyList<>))
                {
                    elementType = c.GetGenericArguments()[0];
                    return true;
                }
            }
            return false;
        }

        private static object BindDictionary(PimlObject obj, Type type, Type valueType, PimlSerializerOptions o)
        {
            var concrete = type.IsInterface || type.IsAbstract
                ? typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType)
                : type;
            var result = Activator.CreateInstance(concrete)
                         ?? throw new PimlException("Cannot create " + concrete.FullName + ".");
            var add = typeof(ICollection<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(typeof(string), valueType)).GetMethod("Add")!;
            var kvpCtor = typeof(KeyValuePair<,>).MakeGenericType(typeof(string), valueType).GetConstructors()[0];
            foreach (var kv in obj)
            {
                var value = Bind(kv.Value, valueType, o);
                add.Invoke(result, new[] { kvpCtor.Invoke(new[] { kv.Key, value }) });
            }
            return result;
        }

        private static object BindCollection(PimlArray arr, Type type, Type elementType, PimlSerializerOptions o)
        {
            if (type.IsArray)
            {
                var array = Array.CreateInstance(elementType, arr.Count);
                for (int i = 0; i < arr.Count; i++) array.SetValue(Bind(arr[i], elementType, o), i);
                return array;
            }
            var concrete = type.IsInterface || type.IsAbstract
                ? typeof(List<>).MakeGenericType(elementType)
                : type;
            var result = Activator.CreateInstance(concrete)
                         ?? throw new PimlException("Cannot create " + concrete.FullName + ".");
            var add = typeof(ICollection<>).MakeGenericType(elementType).GetMethod("Add")!;
            foreach (var item in arr) add.Invoke(result, new[] { Bind(item, elementType, o) });
            return result;
        }

        private static object BindObject(PimlObject obj, Type type, PimlSerializerOptions o)
        {
            var members = TypeInfoCache.Get(type);
            var ctor = type.GetConstructor(Type.EmptyTypes);
            var bound = new HashSet<string>(StringComparer.Ordinal);
            object instance;

            if (ctor != null || type.IsValueType)
            {
                instance = Activator.CreateInstance(type) ?? throw new PimlException("Cannot create " + type.FullName + ".");
            }
            else
            {
                var best = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault()
                           ?? throw new PimlException(type.FullName + " has no public constructor.");
                var parameters = best.GetParameters();
                var args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];
                    var member = members.FirstOrDefault(m => string.Equals(m.Name, p.Name, StringComparison.OrdinalIgnoreCase));
                    var key = member != null ? member.KeyFor(o) : o.KeyNamingPolicy(p.Name!);
                    if (obj.TryGetValue(key, out var valueNode))
                    {
                        args[i] = Bind(valueNode, p.ParameterType, o);
                        bound.Add(key);
                    }
                    else if (p.HasDefaultValue) args[i] = p.DefaultValue;
                    else args[i] = p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
                }
                instance = best.Invoke(args);
            }

            foreach (var member in members)
            {
                if (member.Set == null) continue;
                var key = member.KeyFor(o);
                if (bound.Contains(key)) continue;
                if (!obj.TryGetValue(key, out var valueNode)) continue;
                member.Set(instance, Bind(valueNode, member.Type, o));
            }
            return instance;
        }
    }
}
