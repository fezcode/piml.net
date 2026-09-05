using System;
using Piml.Internal;

namespace Piml
{
    /// <summary>Entry point for parsing, writing and (de)serializing PIML documents.</summary>
    public static class Piml
    {
        /// <summary>The PIML specification version this library implements.</summary>
        public const string SpecVersion = "1.2.0";

        /// <summary>Parses PIML text into an ordered object tree using schemaless type inference.</summary>
        /// <exception cref="PimlSyntaxException">The text violates the PIML grammar.</exception>
        public static PimlObject Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return Parser.Parse(LineScanner.Scan(text), null);
        }

        /// <summary>Writes an object tree as canonical PIML text.</summary>
        public static string Write(PimlObject root, PimlWriterOptions? options = null) => PimlWriter.Write(root, options);

        /// <summary>Serializes an object graph to PIML text. The value must map to an object (not a scalar or list).</summary>
        public static string Serialize<T>(T value, PimlSerializerOptions? options = null)
        {
            var o = options ?? PimlSerializerOptions.Default;
            var node = PimlSerializer.ToNode(value, o);
            if (!(node is PimlObject root))
                throw new PimlException("The root value must serialize to an object; got " + node.Kind + ".");
            return PimlWriter.Write(root, o.Writer);
        }
    }
}
