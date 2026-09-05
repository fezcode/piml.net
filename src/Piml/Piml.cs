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
    }
}
