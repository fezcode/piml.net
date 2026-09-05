using Xunit;

namespace Piml.Tests;

public class DocumentTests
{
    public const string Sample =
        "# fez settings\n" +
        "(editor)\n" +
        "  (fontSize) 14 # px\n" +
        "  (fontFamily) JetBrains Mono\n" +
        "\n" +
        "  (rulers)\n" +
        "    > 80\n" +
        "    > 120\n" +
        "(theme) studio-fluent\n" +
        "(notes)\n" +
        "  first line\n" +
        "  second line\n";

    [Fact]
    public void ToString_returns_original_text_unchanged()
    {
        Assert.Equal(Sample, Piml.ParseDocument(Sample).ToString());
        var crlf = Sample.Replace("\n", "\r\n");
        Assert.Equal(crlf, Piml.ParseDocument(crlf).ToString());
        var noTrailing = Sample.TrimEnd('\n');
        Assert.Equal(noTrailing, Piml.ParseDocument(noTrailing).ToString());
        Assert.Equal("", Piml.ParseDocument("").ToString());
    }

    [Fact]
    public void Root_matches_plain_parse()
    {
        var doc = Piml.ParseDocument(Sample);
        Assert.True(PimlNode.DeepEquals(Piml.Parse(Sample), doc.Root));
        Assert.Equal("\n", doc.NewLine);
        Assert.Equal("\r\n", Piml.ParseDocument("(a) 1\r\n").NewLine);
    }

    [Fact]
    public void Get_navigates_objects_and_arrays()
    {
        var doc = Piml.ParseDocument(Sample);
        Assert.Equal(14L, ((PimlInteger)doc.Get("editor", "fontSize")!).Value);
        Assert.Equal(120L, ((PimlInteger)doc.Get("editor", "rulers", "1")!).Value);
        Assert.Equal("first line\nsecond line", ((PimlString)doc.Get("notes")!).Value);
        Assert.Null(doc.Get("editor", "missing"));
        Assert.Null(doc.Get("editor", "rulers", "9"));
        Assert.Null(doc.Get("theme", "not-an-object"));
        Assert.Same(doc.Root, doc.Get());
    }

    [Fact]
    public void Syntax_errors_surface_from_the_document_parser()
    {
        var ex = Assert.Throws<PimlSyntaxException>(() => Piml.ParseDocument("(a)\n\t(b) 1"));
        Assert.Equal(2, ex.Line);
    }
}
