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

        /// <summary>
        /// Parses PIML text and binds it to <typeparamref name="T"/>. Quoted numbers bind to numeric members,
        /// integers bind to strings, enums bind by name (case-insensitive), <c>nil</c> binds to null / default /
        /// an empty collection, records bind through their primary constructor, and unknown keys are ignored.
        /// </summary>
        public static T? Deserialize<T>(string text, PimlSerializerOptions? options = null) =>
            PimlSerializer.FromNode<T>(Parse(text), options);

        /// <summary>Non-generic form of <see cref="Deserialize{T}"/>.</summary>
        public static object? Deserialize(string text, Type type, PimlSerializerOptions? options = null) =>
            PimlSerializer.FromNode(Parse(text), type, options);

        /// <summary>Parses text into a <see cref="PimlDocument"/> that can be edited and written back without losing comments or formatting.</summary>
        public static PimlDocument ParseDocument(string text) => PimlDocument.Parse(text);
    }
}
