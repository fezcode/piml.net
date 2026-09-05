using System;

namespace Piml
{
    /// <summary>Options for <see cref="PimlSerializer"/>, <see cref="Piml.Serialize{T}"/> and <see cref="Piml.Deserialize{T}"/>.</summary>
    public sealed class PimlSerializerOptions
    {
        /// <summary>The default options: camelCase keys, empty values written.</summary>
        public static PimlSerializerOptions Default { get; } = new PimlSerializerOptions();

        /// <summary>Maps a member name to a PIML key when no <see cref="PimlKeyAttribute"/> is present. Default <see cref="PimlNaming.CamelCase"/>.</summary>
        public Func<string, string> KeyNamingPolicy { get; set; } = PimlNaming.CamelCase;

        /// <summary>When true every member behaves as if marked <see cref="PimlOmitEmptyAttribute"/>.</summary>
        public bool OmitEmptyValues { get; set; }

        /// <summary>Writer formatting used by <see cref="Piml.Serialize{T}"/>.</summary>
        public PimlWriterOptions Writer { get; set; } = new PimlWriterOptions();
    }
}
