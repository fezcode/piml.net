using Xunit;

namespace Piml.Tests;

public class DocumentEditTests
{
    const string Sample = DocumentTests.Sample;

    [Fact]
    public void Replacing_a_scalar_rewrites_only_its_line_and_keeps_the_comment()
    {
        var doc = Piml.ParseDocument(Sample);
        doc.Set(new[] { "editor", "fontSize" }, 16);
        Assert.Equal(Sample.Replace("  (fontSize) 14 # px\n", "  (fontSize) 16 # px\n"), doc.ToString());
        Assert.Equal(16L, ((PimlInteger)doc.Get("editor", "fontSize")!).Value);
    }

    [Fact]
    public void Scalar_replacement_quotes_when_needed()
    {
        var doc = Piml.ParseDocument("(zip) 1\n");
        doc.Set("zip", "08080");
        Assert.Equal("(zip) 08080\n", doc.ToString());
        doc.Set("zip", "123");
        Assert.Equal("(zip) \"123\"\n", doc.ToString());
        doc.Set("zip", PimlNull.Instance);
        Assert.Equal("(zip) nil\n", doc.ToString());
    }

    [Fact]
    public void Adding_a_key_appends_after_the_parents_last_line()
    {
        var doc = Piml.ParseDocument(Sample);
        doc.Set(new[] { "editor", "tabSize" }, 4);
        var expected = Sample.Replace("    > 120\n", "    > 120\n  (tabSize) 4\n");
        Assert.Equal(expected, doc.ToString());
    }

    [Fact]
    public void Adding_a_root_key_appends_at_the_end()
    {
        var doc = Piml.ParseDocument(Sample);
        doc.Set("version", 2);
        Assert.Equal(Sample + "(version) 2\n", doc.ToString());

        var noTrailing = Piml.ParseDocument("(a) 1");
        noTrailing.Set("b", 2);
        Assert.Equal("(a) 1\n(b) 2", noTrailing.ToString());
    }

    [Fact]
    public void Replacing_a_block_with_a_block()
    {
        var doc = Piml.ParseDocument(Sample);
        doc.Set(new[] { "editor", "rulers" }, new PimlArray { 100 });
        var expected = Sample.Replace("  (rulers)\n    > 80\n    > 120\n", "  (rulers)\n    > 100\n");
        Assert.Equal(expected, doc.ToString());
    }

    [Fact]
    public void Scalar_to_block_and_block_to_scalar()
    {
        var doc = Piml.ParseDocument("(a) 1\n(b)\n  (x) 1\n  (y) 2\n(c) 3\n");
        doc.Set("a", new PimlObject { { "k", "v" } });
        doc.Set("b", "flat");
        Assert.Equal("(a)\n  (k) v\n(b) flat\n(c) 3\n", doc.ToString());
    }

    [Fact]
    public void Multiline_strings_are_blocks()
    {
        var doc = Piml.ParseDocument(Sample);
        doc.Set("notes", "only one line");
        Assert.Equal(Sample.Replace("(notes)\n  first line\n  second line\n", "(notes) only one line\n"), doc.ToString());
        doc.Set("theme", "a\nb");
        Assert.Contains("(theme)\n  a\n  b\n", doc.ToString());
    }

    [Fact]
    public void Missing_parents_are_created_and_nil_parents_become_objects()
    {
        var doc = Piml.ParseDocument("(a) 1\n(empty) nil\n(bare)\n");
        doc.Set(new[] { "new", "deep", "key" }, true);
        doc.Set(new[] { "empty", "k" }, 1);
        doc.Set(new[] { "bare", "k" }, 2);
        Assert.Equal("(a) 1\n(empty)\n  (k) 1\n(bare)\n  (k) 2\n(new)\n  (deep)\n    (key) true\n", doc.ToString());
    }

    [Fact]
    public void Array_items_can_be_replaced_and_appended()
    {
        var doc = Piml.ParseDocument(Sample);
        doc.Set(new[] { "editor", "rulers", "0" }, 72);
        doc.Set(new[] { "editor", "rulers", "2" }, 160);
        var expected = Sample.Replace("    > 80\n    > 120\n", "    > 72\n    > 120\n    > 160\n");
        Assert.Equal(expected, doc.ToString());
        Assert.Throws<PimlException>(() => doc.Set(new[] { "editor", "rulers", "9" }, 1));
    }

    [Fact]
    public void Remove_deletes_the_nodes_lines_only()
    {
        var doc = Piml.ParseDocument(Sample);
        Assert.True(doc.Remove("editor", "rulers"));
        Assert.False(doc.Remove("editor", "rulers"));
        Assert.Equal(Sample.Replace("  (rulers)\n    > 80\n    > 120\n", ""), doc.ToString());
        Assert.True(doc.Remove("editor", "fontSize"));
        Assert.Contains("(editor)\n  (fontFamily) JetBrains Mono\n", doc.ToString());
    }

    [Fact]
    public void Removing_the_last_child_leaves_a_bare_key()
    {
        var doc = Piml.ParseDocument("(p)\n  (x) 1\n(q) 2\n");
        doc.Remove("p", "x");
        Assert.Equal("(p)\n(q) 2\n", doc.ToString());
        Assert.Equal(PimlNodeKind.Null, doc.Get("p")!.Kind);
    }

    [Fact]
    public void Edits_keep_crlf()
    {
        var doc = Piml.ParseDocument("(a) 1\r\n(b) 2\r\n");
        doc.Set("c", new PimlArray { 1 });
        Assert.Equal("(a) 1\r\n(b) 2\r\n(c)\r\n  > 1\r\n", doc.ToString());
    }

    [Fact]
    public void Empty_path_is_rejected()
    {
        var doc = Piml.ParseDocument("");
        Assert.Throws<ArgumentException>(() => doc.Set(Array.Empty<string>(), 1));
    }
}
