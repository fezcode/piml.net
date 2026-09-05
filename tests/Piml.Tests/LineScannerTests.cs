using Piml.Internal;
using Xunit;

namespace Piml.Tests;

public class LineScannerTests
{
    // LineKind is internal, so the expected kind is passed by name (public test methods
    // cannot expose internal parameter types).
    [Theory]
    [InlineData("(key) value", 0, "Key", "(key) value")]
    [InlineData("  > item", 2, "Item", "> item")]
    [InlineData("    # note", 4, "Comment", "# note")]
    [InlineData("  plain text", 2, "Content", "plain text")]
    [InlineData("", 0, "Blank", "")]
    [InlineData("     ", 5, "Blank", "")]
    [InlineData("  \\# escaped", 2, "Content", "\\# escaped")]
    public void Classifies_lines(string raw, int indent, string kind, string text)
    {
        var line = LineScanner.Classify(0, raw);
        Assert.Equal(indent, line.Indent);
        Assert.Equal(kind, line.Kind.ToString());
        Assert.Equal(text, line.Text);
    }

    [Fact]
    public void Scan_strips_carriage_returns_and_numbers_lines()
    {
        var lines = LineScanner.Scan("(a) 1\r\n(b) 2\r\n");
        Assert.Equal(3, lines.Count);
        Assert.Equal("(a) 1", lines[0].Raw);
        Assert.Equal("(b) 2", lines[1].Raw);
        Assert.Equal(LineKind.Blank, lines[2].Kind);
        Assert.Equal(2, lines[1].Number);
        Assert.Equal(1, lines[1].Index);
    }

    [Fact]
    public void Tab_in_indentation_is_a_syntax_error_with_position()
    {
        var ex = Assert.Throws<PimlSyntaxException>(() => LineScanner.Scan("(a)\n\t(b) 1"));
        Assert.Equal(2, ex.Line);
        Assert.Equal(1, ex.Column);
        Assert.Contains("Tabs", ex.Message);
    }

    [Fact]
    public void Tab_after_content_is_not_an_indentation_error()
    {
        var lines = LineScanner.Scan("(a) x\ty");
        Assert.Equal(LineKind.Key, lines[0].Kind);
    }

    [Fact]
    public void Whitespace_only_line_with_tab_is_blank()
    {
        var lines = LineScanner.Scan(" \t ");
        Assert.Equal(LineKind.Blank, lines[0].Kind);
    }
}
