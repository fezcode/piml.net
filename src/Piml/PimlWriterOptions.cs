namespace Piml
{
    /// <summary>Formatting options for <see cref="PimlWriter"/>.</summary>
    public sealed class PimlWriterOptions
    {
        /// <summary>Line terminator; default <c>\n</c>.</summary>
        public string NewLine { get; set; } = "\n";

        /// <summary>Label written after <c>&gt;</c> for object items when <see cref="LabelObjectItems"/> is true; default <c>item</c>.</summary>
        public string ItemLabel { get; set; } = "item";

        /// <summary>Write object items as <c>&gt; (item)</c> (true, default) or as a bare <c>&gt;</c> (false).</summary>
        public bool LabelObjectItems { get; set; } = true;

        internal static readonly PimlWriterOptions Default = new PimlWriterOptions();
    }
}
