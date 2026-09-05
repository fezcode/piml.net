using Xunit;

namespace Piml.Tests;

public class WriterTests
{
    [Fact]
    public void Writes_canonical_document()
    {
        var root = new PimlObject
        {
            { "site_name", "My Awesome Site" },
            { "port", 8080 },
            { "is_production", true },
            { "ratio", 1.5 },
            { "admins", new PimlArray
                {
                    new PimlObject { { "id", 1 }, { "name", "Admin One" } },
                    new PimlObject { { "id", 2 }, { "name", "Admin Two" } },
                } },
            { "features", new PimlArray { "auth", "logging" } },
            { "matrix", new PimlArray { new PimlArray { 1, 2 }, new PimlArray { 3 } } },
            { "description", "This is a multi-line\ndescription for the site." },
            { "metadata", new PimlObject() },
            { "related_ids", new PimlArray() },
            { "nothing", PimlNull.Instance },
        };

        var expected =
            "(site_name) My Awesome Site\n" +
            "(port) 8080\n" +
            "(is_production) true\n" +
            "(ratio) 1.5\n" +
            "(admins)\n" +
            "  > (item)\n" +
            "    (id) 1\n" +
            "    (name) Admin One\n" +
            "  > (item)\n" +
            "    (id) 2\n" +
            "    (name) Admin Two\n" +
            "(features)\n" +
            "  > auth\n" +
            "  > logging\n" +
            "(matrix)\n" +
            "  >\n" +
            "    > 1\n" +
            "    > 2\n" +
            "  >\n" +
            "    > 3\n" +
            "(description)\n" +
            "  This is a multi-line\n" +
            "  description for the site.\n" +
            "(metadata) nil\n" +
            "(related_ids) nil\n" +
            "(nothing) nil\n";

        Assert.Equal(expected, Piml.Write(root));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(" padded ", true)]
    [InlineData("nil", true)]
    [InlineData("true", true)]
    [InlineData("false", true)]
    [InlineData("123", true)]
    [InlineData("-1.5", true)]
    [InlineData("\"quoted", true)]
    [InlineData("#hash", true)]
    [InlineData("a \\# b", true)]
    [InlineData("a # b", true)]
    [InlineData("TRUE", false)]
    [InlineData("08080", false)]
    [InlineData("a#b", false)]
    [InlineData("say \"hi\"", false)]
    [InlineData("(looks like key)", false)]
    public void NeedsQuoting_matches_reference_rules(string value, bool expected)
    {
        Assert.Equal(expected, PimlWriter.NeedsQuoting(value));
    }

    [Theory]
    [InlineData(1.0, "1.0")]
    [InlineData(99.99, "99.99")]
    [InlineData(-0.5, "-0.5")]
    [InlineData(0.1, "0.1")]
    [InlineData(1e21, "1000000000000000000000.0")]
    [InlineData(1e-7, "0.0000001")]
    public void Formats_floats_without_exponents(double value, string expected)
    {
        Assert.Equal(expected, PimlWriter.FormatScalar(new PimlFloat(value)));
    }

    [Fact]
    public void Quoted_scalars_and_escaped_blocks_round_trip()
    {
        var root = new PimlObject
        {
            { "zip", "08080" },
            { "empty", "" },
            { "word", "true" },
            { "hash", "#ff0000" },
            { "comment", "five # six" },
            { "block", "# starts with hash\n(x) y\n> z\n\n  indented\ntrailing spaces   " },
            { "items", new PimlArray { "nil", "(label)", "line1\nline2" } },
        };
        var text = Piml.Write(root);
        var again = Piml.Parse(text);
        Assert.Equal("08080", ((PimlString)again["zip"]).Value);
        Assert.Equal("", ((PimlString)again["empty"]).Value);
        Assert.Equal("true", ((PimlString)again["word"]).Value);
        Assert.Equal("#ff0000", ((PimlString)again["hash"]).Value);
        Assert.Equal("five # six", ((PimlString)again["comment"]).Value);
        Assert.Equal("# starts with hash\n(x) y\n> z\n\n  indented\ntrailing spaces", ((PimlString)again["block"]).Value);
        Assert.Equal("nil", ((PimlString)((PimlArray)again["items"])[0]).Value);
        Assert.Equal("(label)", ((PimlString)((PimlArray)again["items"])[1]).Value);
        Assert.Equal("line1\nline2", ((PimlString)((PimlArray)again["items"])[2]).Value);
    }

    [Fact]
    public void Honors_newline_and_label_options()
    {
        var root = new PimlObject { { "a", new PimlArray { new PimlObject { { "k", 1 } } } } };
        var text = Piml.Write(root, new PimlWriterOptions { NewLine = "\r\n", LabelObjectItems = false });
        Assert.Equal("(a)\r\n  >\r\n    (k) 1\r\n", text);
    }

    [Fact]
    public void Rejects_nan_and_infinity()
    {
        Assert.Throws<PimlException>(() => Piml.Write(new PimlObject { { "x", double.NaN } }));
        Assert.Throws<PimlException>(() => Piml.Write(new PimlObject { { "x", double.PositiveInfinity } }));
    }
}
