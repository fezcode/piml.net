using Xunit;

namespace Piml.Tests;

public class ParserObjectTests
{
    [Fact]
    public void Parses_multiple_roots_in_order()
    {
        var o = Piml.Parse("(a) 1\n(b) 2");
        Assert.Equal(new[] { "a", "b" }, o.Keys);
        Assert.Equal(1L, ((PimlInteger)o["a"]).Value);
        Assert.Equal(2L, ((PimlInteger)o["b"]).Value);
    }

    [Fact]
    public void Parses_nested_objects()
    {
        var o = Piml.Parse("(l1)\n  (l2)\n    (l3)\n      (name) Deep");
        var l3 = (PimlObject)((PimlObject)((PimlObject)o["l1"])["l2"])["l3"];
        Assert.Equal("Deep", ((PimlString)l3["name"]).Value);
    }

    [Fact]
    public void Bare_key_is_nil_and_keys_may_contain_spaces()
    {
        var o = Piml.Parse("(my key)\n(other) # todo");
        Assert.Equal(PimlNodeKind.Null, o["my key"].Kind);
        Assert.Equal(PimlNodeKind.Null, o["other"].Kind);
    }

    [Fact]
    public void Comments_and_blank_lines_are_ignored_at_any_indent()
    {
        var o = Piml.Parse("# top\n(parent)\n\n  # a note\n  (child) 1\n   # odd indent comment\n(next) 2\n");
        Assert.Equal(1L, ((PimlInteger)((PimlObject)o["parent"])["child"]).Value);
        Assert.Equal(2L, ((PimlInteger)o["next"]).Value);
    }

    [Fact]
    public void Accepts_crlf()
    {
        var o = Piml.Parse("(a) 1\r\n(b) 2\r\n");
        Assert.Equal(2, o.Count);
    }

    [Fact]
    public void Empty_document_is_empty_object()
    {
        Assert.Equal(0, Piml.Parse("").Count);
        Assert.Equal(0, Piml.Parse("# only a comment\n\n").Count);
    }

    [Theory]
    [InlineData("(a)\n   (b) 1", 2, "multiple of 2")]
    [InlineData("(a)\n    (b) 1", 2, "exactly one level")]
    [InlineData("(key value", 1, "closing")]
    [InlineData("(ke(y) v", 1, "parenthes")]
    [InlineData("() v", 1, "empty")]
    [InlineData("(a) 1\n(a) 2", 2, "Duplicate key 'a'")]
    [InlineData("(p)\n  (x) 1\n  (x) 2", 3, "Duplicate key 'x'")]
    [InlineData("> item", 1, "root")]
    [InlineData("(parent)\n  (a) 1\n  > x", 3, "inside an object")]
    [InlineData("(a) 1\n  (b) 2", 2, "inline value")]
    [InlineData("just text", 1, "Expected a (key)")]
    public void Reports_syntax_errors_with_line(string piml, int line, string messagePart)
    {
        var ex = Assert.Throws<PimlSyntaxException>(() => Piml.Parse(piml));
        Assert.Equal(line, ex.Line);
        Assert.Contains(messagePart, ex.Message);
    }

    [Fact]
    public void Key_may_be_followed_by_value_containing_parens()
    {
        var o = Piml.Parse("(fn) call(a, b)");
        Assert.Equal("call(a, b)", ((PimlString)o["fn"]).Value);
    }
}
