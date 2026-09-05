using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Piml.Internal
{
    internal sealed class TypeMember
    {
        public TypeMember(string name, Type type, Func<object, object?> get, Action<object, object?>? set, string? explicitKey, bool omitEmpty)
        {
            Name = name; Type = type; Get = get; Set = set; ExplicitKey = explicitKey; OmitEmpty = omitEmpty;
        }
        public string Name { get; }
        public Type Type { get; }
        public Func<object, object?> Get { get; }
        public Action<object, object?>? Set { get; }
        public string? ExplicitKey { get; }
        public bool OmitEmpty { get; }
        public string KeyFor(PimlSerializerOptions options) => ExplicitKey ?? options.KeyNamingPolicy(Name);
    }

    internal static class TypeInfoCache
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<TypeMember>> Cache =
            new ConcurrentDictionary<Type, IReadOnlyList<TypeMember>>();

        /// <summary>Public instance properties (readable, non-indexed) and fields in declaration order, minus [PimlIgnore].</summary>
        public static IReadOnlyList<TypeMember> Get(Type type) => Cache.GetOrAdd(type, Build);

        private static IReadOnlyList<TypeMember> Build(Type type)
        {
            var members = new List<(int Token, TypeMember Member)>();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (var p in type.GetProperties(flags))
            {
                if (!p.CanRead || p.GetIndexParameters().Length != 0) continue;
                if (p.GetCustomAttribute<PimlIgnoreAttribute>() != null) continue;
                var getter = p.GetGetMethod();
                if (getter == null) continue;
                var setter = p.GetSetMethod(nonPublic: true);
                Action<object, object?>? set = setter == null ? null : (obj, v) => p.SetValue(obj, v);
                members.Add((p.MetadataToken, new TypeMember(
                    p.Name, p.PropertyType, obj => p.GetValue(obj), set,
                    p.GetCustomAttribute<PimlKeyAttribute>()?.Name,
                    p.GetCustomAttribute<PimlOmitEmptyAttribute>() != null)));
            }

            foreach (var f in type.GetFields(flags))
            {
                if (f.GetCustomAttribute<PimlIgnoreAttribute>() != null) continue;
                Action<object, object?>? set = f.IsInitOnly ? null : (obj, v) => f.SetValue(obj, v);
                members.Add((f.MetadataToken, new TypeMember(
                    f.Name, f.FieldType, obj => f.GetValue(obj), set,
                    f.GetCustomAttribute<PimlKeyAttribute>()?.Name,
                    f.GetCustomAttribute<PimlOmitEmptyAttribute>() != null)));
            }

            return members.OrderBy(m => m.Token).Select(m => m.Member).ToList();
        }
    }
}
