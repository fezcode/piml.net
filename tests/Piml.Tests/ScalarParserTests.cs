using Piml.Internal;
using Xunit;

namespace Piml.Tests;

public class ScalarParserTests
{
    [Theory]
    [InlineData(" value", PimlNodeKind.String, "value")]
    [InlineData(" 123", PimlNodeKind.Integer, "123")]
    [InlineData(" -42", PimlNodeKind.Integer, "-42")]
    [InlineData(" 99.99", PimlNodeKind.Float, "99.99")]
    [InlineData(" -0.5", PimlNodeKind.Float, "-0.5")]
    [InlineData(" true", PimlNodeKind.Boolean, "true")]
    [InlineData(" false", PimlNodeKind.Boolean, "false")]
    [InlineData(" nil", PimlNodeKind.Null, "nil")]
    [InlineData(" TRUE", PimlNodeKind.String, "TRUE")]
    [InlineData(" 08080", PimlNodeKind.String, "08080")]
    [InlineData(" +5", PimlNodeKind.String, "+5")]
    [InlineData(" 1e5", PimlNodeKind.String, "1e5")]
    [InlineData(" \"123\"", PimlNodeKind.String, "123")]
    [InlineData(" \"true\"", PimlNodeKind.String, "true")]
    [InlineData(" \"nil\"", PimlNodeKind.String, "nil")]
    [InlineData(" \"\"", PimlNodeKind.String, "")]
    [InlineData(" \"  padded  \"", PimlNodeKind.String, "  padded  ")]
    [InlineData(" \"say \"hi\"\"", PimlNodeKind.String, "say \"hi\"")]
    [InlineData(" \"a # b\"", PimlNodeKind.String, "a # b")]
    [InlineData(" \"abc\" # note", PimlNodeKind.String, "abc")]
    [InlineData(" \"abc", PimlNodeKind.String, "\"abc")]
    [InlineData(" \"Errare humanum est\" is a Latin proverb", PimlNodeKind.String, "\"Errare humanum est\" is a Latin proverb")]
    [InlineData(" \"\"Every monster was a man first.\"\"", PimlNodeKind.String, "\"Every monster was a man first.\"")]
    [InlineData("    hello   ", PimlNodeKind.String, "hello")]
    [InlineData(" localhost # dev box", PimlNodeKind.String, "localhost")]
    [InlineData(" https://x.com/a#b", PimlNodeKind.String, "https://x.com/a#b")]
    [InlineData(" five \\# six", PimlNodeKind.String, "five # six")]
    [InlineData(" \\#ff0000", PimlNodeKind.String, "#ff0000")]
    [InlineData(" \"#ff0000\"", PimlNodeKind.String, "#ff0000")]
    [InlineData("", PimlNodeKind.Null, "nil")]
    [InlineData(" # todo: fill in", PimlNodeKind.Null, "nil")]
    [InlineData(" 9223372036854775808", PimlNodeKind.String, "9223372036854775808")]
    public void Parses_scalars_per_spec(string raw, PimlNodeKind kind, string text)
    {
        var node = ScalarParser.Parse(raw);
        Assert.Equal(kind, node.Kind);
        Assert.Equal(text, node.ToString());
    }

    [Theory]
    [InlineData(" localhost # dev box", "# dev box")]
    [InlineData(" \"abc\" # note", "# note")]
    [InlineData(" # todo", "# todo")]
    [InlineData(" plain", "")]
    [InlineData(" \"a # b\"", "")]
    public void Extracts_inline_comment(string raw, string expectedComment)
    {
        ScalarParser.Parse(raw, out var comment);
        Assert.Equal(expectedComment, comment);
    }

    [Theory]
    [InlineData("a #b", 2)]
    [InlineData("a\t#b", 2)]
    [InlineData("a#b", -1)]
    [InlineData("#b", -1)]
    [InlineData("a \\#b", -1)]
    public void Finds_inline_comment_only_after_whitespace(string text, int index)
    {
        Assert.Equal(index, ScalarParser.FindInlineComment(text));
    }
}
