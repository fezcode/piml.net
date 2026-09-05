using Xunit;

namespace Piml.Tests;

public class SerializerFromNodeTests
{
    public enum Level { Debug, Info }

    public sealed class Admin
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class Config
    {
        public string SiteName { get; set; } = "";
        public int Port { get; set; }
        public bool IsProduction { get; set; }
        public List<Admin> Admins { get; set; } = new();
        public string[] Features { get; set; } = Array.Empty<string>();
        public DateTime LastUpdated { get; set; }
        public string Description { get; set; } = "";
        [PimlKey("log level")] public Level LogLevel { get; set; }
        [PimlIgnore] public string Secret { get; set; } = "hidden";
        public Dictionary<string, int> Limits { get; set; } = new();
        public double? Ratio { get; set; }
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
        public HashSet<int> Ids { get; set; } = new();
        public Uri? Home { get; set; }
    }

    public sealed record Point(int X, int Y, string Label = "origin");

    const string Doc =
        "(siteName) My Awesome Site\n" +
        "(port) \"8080\"\n" +
        "(isProduction) true\n" +
        "(admins)\n" +
        "  > (User)\n" +
        "    (id) 1\n" +
        "    (name) Admin One\n" +
        "  >\n" +
        "    (id) 2\n" +
        "    (name) 42\n" +
        "(features)\n" +
        "  > auth\n" +
        "  > logging\n" +
        "(lastUpdated) 2023-11-10T15:30:00Z\n" +
        "(description)\n" +
        "  This is a multi-line\n" +
        "  description for the site.\n" +
        "(log level) info\n" +
        "(secret) leaked?\n" +
        "(limits)\n" +
        "  (cpu) 2\n" +
        "(ratio) nil\n" +
        "(tags) nil\n" +
        "(ids)\n" +
        "  > 3\n" +
        "  > 3\n" +
        "  > 4\n" +
        "(home) https://fezcode.com\n" +
        "(unknownKey) ignored\n";

    [Fact]
    public void Deserializes_object_graph_with_coercion()
    {
        var cfg = Piml.Deserialize<Config>(Doc)!;
        Assert.Equal("My Awesome Site", cfg.SiteName);
        Assert.Equal(8080, cfg.Port);
        Assert.True(cfg.IsProduction);
        Assert.Equal(2, cfg.Admins.Count);
        Assert.Equal("Admin One", cfg.Admins[0].Name);
        Assert.Equal("42", cfg.Admins[1].Name);
        Assert.Equal(new[] { "auth", "logging" }, cfg.Features);
        Assert.Equal(new DateTime(2023, 11, 10, 15, 30, 0, DateTimeKind.Utc), cfg.LastUpdated.ToUniversalTime());
        Assert.Equal("This is a multi-line\ndescription for the site.", cfg.Description);
        Assert.Equal(Level.Info, cfg.LogLevel);
        Assert.Equal("hidden", cfg.Secret);
        Assert.Equal(2, cfg.Limits["cpu"]);
        Assert.Null(cfg.Ratio);
        Assert.Empty(cfg.Tags);
        Assert.Equal(new HashSet<int> { 3, 4 }, cfg.Ids);
        Assert.Equal("https://fezcode.com", cfg.Home!.OriginalString);
    }

    [Fact]
    public void Binds_records_through_the_primary_constructor()
    {
        var p = Piml.Deserialize<Point>("(x) 1\n(y) 2")!;
        Assert.Equal(new Point(1, 2), p);
        var q = Piml.Deserialize<Point>("(x) 1\n(y) 2\n(label) here")!;
        Assert.Equal("here", q.Label);
    }

    [Fact]
    public void Deserializes_to_object_and_node_targets()
    {
        var clr = Piml.Deserialize<object>("(a) 1\n(b) 2.5\n(c) true\n(d) nil\n(e)\n  > x\n(f)\n  (g) s") as Dictionary<string, object?>;
        Assert.NotNull(clr);
        Assert.Equal(1L, clr!["a"]);
        Assert.Equal(2.5, clr["b"]);
        Assert.Equal(true, clr["c"]);
        Assert.Null(clr["d"]);
        Assert.Equal(new List<object?> { "x" }, clr["e"]);
        Assert.Equal("s", ((Dictionary<string, object?>)clr["f"]!)["g"]);

        var node = Piml.Deserialize<PimlObject>("(a) 1")!;
        Assert.Equal(1L, ((PimlInteger)node["a"]).Value);
        var str = PimlSerializer.FromNode<PimlString>(new PimlString("v"));
        Assert.Equal("v", str!.Value);
    }

    [Fact]
    public void Dictionaries_of_objects_and_nested_lists()
    {
        var d = Piml.Deserialize<Dictionary<string, Admin>>("(a)\n  (id) 1\n  (name) A\n(b)\n  (id) 2\n  (name) B")!;
        Assert.Equal("B", d["b"].Name);
        var m = Piml.Deserialize<Dictionary<string, List<List<int>>>>("(matrix)\n  >\n    > 1\n    > 2\n  >\n    > 3")!;
        Assert.Equal(new List<List<int>> { new() { 1, 2 }, new() { 3 } }, m["matrix"]);
    }

    [Theory]
    [InlineData("(port) abc")]
    [InlineData("(port) 1.5")]
    [InlineData("(port)\n  > 1")]
    [InlineData("(isProduction) yes")]
    [InlineData("(admins) text")]
    [InlineData("(log level) verbose")]
    public void Reports_type_mismatches(string piml)
    {
        Assert.Throws<PimlException>(() => Piml.Deserialize<Config>(piml));
    }

    [Fact]
    public void Value_type_targets_get_defaults_for_nil()
    {
        var cfg = Piml.Deserialize<Config>("(port) nil\n(isProduction) nil\n(lastUpdated) nil")!;
        Assert.Equal(0, cfg.Port);
        Assert.False(cfg.IsProduction);
        Assert.Equal(default, cfg.LastUpdated);
    }
}
