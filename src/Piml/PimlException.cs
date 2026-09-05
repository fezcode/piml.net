using System;

namespace Piml
{
    /// <summary>Base exception for PIML errors.</summary>
    public class PimlException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public PimlException(string message) : base(message) { }
        /// <summary>Creates the exception with an inner cause.</summary>
        public PimlException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>A syntax error while parsing PIML text. <see cref="Line"/> and <see cref="Column"/> are 1-based.</summary>
    public sealed class PimlSyntaxException : PimlException
    {
        /// <summary>Creates the exception; the message is suffixed with the position.</summary>
        public PimlSyntaxException(string message, int line, int column)
            : base(message + " (line " + line + ", column " + column + ")")
        {
            Line = line;
            Column = column;
        }

        /// <summary>1-based line number.</summary>
        public int Line { get; }
        /// <summary>1-based column number.</summary>
        public int Column { get; }
    }
}
