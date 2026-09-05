using Xunit;

namespace Piml.Tests;

public class ParserMultilineTests
{
    static string S(string piml, string key = "desc") => ((PimlString)Piml.Parse(piml)[key]).Value;

    [Fact] public void Basic() => Assert.Equal("Line 1\nLine 2", S("(desc)\n  Line 1\n  Line 2"));
    [Fact] public void Blank_interior_line() => Assert.Equal("Line 1\n\nLine 3", S("(desc)\n  Line 1\n\n  Line 3"));
    [Fact] public void Whitespace_only_interior_line() => Assert.Equal("Line 1\n\nLine 3", S("(desc)\n  Line 1\n     \n  Line 3"));
    [Fact] public void Trailing_spaces_trimmed() => Assert.Equal("Line 1", S("(desc)\n  Line 1   "));
    [Fact] public void Deeper_indent_preserved() => Assert.Equal("def f():\n    return 1", S("(code)\n  def f():\n      return 1", "code"));
    [Fact] public void Key_lookalike_kept() => Assert.Equal("some text\n(not a key) really", S("(desc)\n  some text\n  (not a key) really"));
    [Fact] public void Item_lookalike_kept() => Assert.Equal("some text\n> not an item", S("(desc)\n  some text\n  > not an item"));
    [Fact] public void Comment_dropped_escaped_hash_kept() => Assert.Equal("Line 1\n# literal hash line\nLine 4", S("(desc)\n  Line 1\n  # a comment, dropped\n  \\# literal hash line\n  Line 4"));
    [Fact] public void First_line_escaped_paren() => Assert.Equal("(x) y", S("(desc)\n  \\(x) y"));
    [Fact] public void First_line_escaped_arrow() => Assert.Equal("> go", S("(desc)\n  \\> go"));
    [Fact] public void Inline_hash_is_literal() => Assert.Equal("price is 5 # not a comment", S("(desc)\n  price is 5 # not a comment"));
    [Fact] public void Backslashes_elsewhere_are_literal() => Assert.Equal("D:\\new\\temp", S("(desc)\n  D:\\new\\temp"));

    [Fact]
    public void Trailing_blank_lines_trimmed_and_next_key_parsed()
    {
        var o = Piml.Parse("(desc)\n  Line 1\n\n\n(next) 1");
        Assert.Equal("Line 1", ((PimlString)o["desc"]).Value);
        Assert.Equal(1L, ((PimlInteger)o["next"]).Value);
    }

    [Fact]
    public void Shallow_comment_does_not_end_block()
    {
        var o = Piml.Parse("(desc)\n  Line 1\n# comment at column 0\n  Line 2\n(next) 1");
        Assert.Equal("Line 1\nLine 2", ((PimlString)o["desc"]).Value);
        Assert.Equal(2, o.Count);
    }

    [Fact]
    public void Multiline_item_in_list()
    {
        var a = (PimlArray)Piml.Parse("(list)\n  >\n    Line 1\n    Line 2")["list"];
        Assert.Equal("Line 1\nLine 2", ((PimlString)a[0]).Value);
    }

    [Fact]
    public void Under_indented_line_is_an_error()
    {
        var ex = Assert.Throws<PimlSyntaxException>(() => Piml.Parse("(desc)\n  Line 1\n Line 2"));
        Assert.Equal(3, ex.Line);
        Assert.Contains("base indentation", ex.Message);
    }
}
