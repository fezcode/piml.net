using System;

namespace Piml
{
    /// <summary>Overrides the PIML key used for a property or field.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class PimlKeyAttribute : Attribute
    {
        /// <summary>Creates the attribute.</summary>
        public PimlKeyAttribute(string name) { Name = name; }
        /// <summary>The key to write and read.</summary>
        public string Name { get; }
    }

    /// <summary>Excludes a property or field from serialization and deserialization.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class PimlIgnoreAttribute : Attribute { }

    /// <summary>Skips the member when its value is null, empty, false, or zero (go-piml's <c>omitempty</c>).</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class PimlOmitEmptyAttribute : Attribute { }
}
