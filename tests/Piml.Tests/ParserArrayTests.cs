using Xunit;

namespace Piml.Tests;

public class ParserArrayTests
{
    [Fact]
    public void Parses_simple_and_typed_items()
    {
        var o = Piml.Parse("(list)\n  > item1\n  > 1\n  > 2.5\n  > true\n  > nil\n  > \"1\"\n  >");
        var a = (PimlArray)o["list"];
        Assert.Equal(7, a.Count);
        Assert.Equal("item1", ((PimlString)a[0]).Value);
        Assert.Equal(1L, ((PimlInteger)a[1]).Value);
        Assert.Equal(2.5, ((PimlFloat)a[2]).Value);
        Assert.True(((PimlBoolean)a[3]).Value);
        Assert.Equal(PimlNodeKind.Null, a[4].Kind);
        Assert.Equal("1", ((PimlString)a[5]).Value);
        Assert.Equal(PimlNodeKind.Null, a[6].Kind);
    }

    [Fact]
    public void Item_inline_comment_and_blank_lines()
    {
        var o = Piml.Parse("(list)\n  > auth # main feature\n\n  > b\n");
        var a = (PimlArray)o["list"];
        Assert.Equal(new[] { "auth", "b" }, a.Select(n => ((PimlString)n).Value));
    }

    [Fact]
    public void Labeled_and_bare_object_items()
    {
        var labeled = (PimlArray)Piml.Parse("(users)\n  > (user)\n    (name) Alice\n  > (user)\n    (name) Bob")["users"];
        var bare = (PimlArray)Piml.Parse("(users)\n  >\n    (name) Alice\n  >\n    (name) Bob")["users"];
        Assert.True(PimlNode.DeepEquals(labeled, bare));
        Assert.Equal("Bob", ((PimlString)((PimlObject)labeled[1])["name"]).Value);
    }

    [Fact]
    public void Labeled_item_without_block_is_a_string()
    {
        var a = (PimlArray)Piml.Parse("(list)\n  > (solo)")["list"];
        Assert.Equal("(solo)", ((PimlString)a[0]).Value);
    }

    [Fact]
    public void Nested_lists()
    {
        var m = (PimlArray)Piml.Parse("(matrix)\n  >\n    > 1\n    > 2\n  >\n    > 3")["matrix"];
        Assert.Equal(2, m.Count);
        Assert.Equal(2, ((PimlArray)m[0]).Count);
        Assert.Equal(3L, ((PimlInteger)((PimlArray)m[1])[0]).Value);
    }

    [Theory]
    [InlineData("(list)\n  > x\n  (b) 2", 3, "not allowed inside an array")]
    [InlineData("(list)\n  > x\n    (b) 2", 3, "inline value")]
    [InlineData("(list)\n  > a\n   > b", 3, "multiple of 2")]
    public void Reports_array_errors(string piml, int line, string messagePart)
    {
        var ex = Assert.Throws<PimlSyntaxException>(() => Piml.Parse(piml));
        Assert.Equal(line, ex.Line);
        Assert.Contains(messagePart, ex.Message);
    }
}
