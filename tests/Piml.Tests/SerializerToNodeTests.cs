using Xunit;

namespace Piml.Tests;

public class SerializerToNodeTests
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
        [PimlOmitEmpty] public string OptionalVal { get; set; } = "";
        [PimlKey("log level")] public Level LogLevel { get; set; }
        [PimlIgnore] public string Secret { get; set; } = "hidden";
        public Dictionary<string, int> Limits { get; set; } = new();
        public double? Ratio { get; set; }
        public Guid Id { get; set; }
    }

    [Fact]
    public void Serializes_object_graph_with_naming_attributes_and_omit_empty()
    {
        var cfg = new Config
        {
            SiteName = "My Awesome Site",
            Port = 8080,
            IsProduction = true,
            Admins = { new Admin { Id = 1, Name = "Admin One" }, new Admin { Id = 2, Name = "Admin Two" } },
            Features = new[] { "auth", "logging" },
            LastUpdated = new DateTime(2023, 11, 10, 15, 30, 0, DateTimeKind.Utc),
            Description = "This is a multi-line\ndescription for the site.",
            OptionalVal = "",
            LogLevel = Level.Info,
            Limits = new Dictionary<string, int> { ["cpu"] = 2, ["mem"] = 512 },
            Ratio = null,
            Id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
        };

        var expected =
            "(siteName) My Awesome Site\n" +
            "(port) 8080\n" +
            "(isProduction) true\n" +
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
            "(lastUpdated) 2023-11-10T15:30:00Z\n" +
            "(description)\n" +
            "  This is a multi-line\n" +
            "  description for the site.\n" +
            "(log level) Info\n" +
            "(limits)\n" +
            "  (cpu) 2\n" +
            "  (mem) 512\n" +
            "(ratio) nil\n" +
            "(id) 0f8fad5b-d9cb-469f-a165-70867728950e\n";

        Assert.Equal(expected, Piml.Serialize(cfg));
    }

    [Fact]
    public void Snake_case_policy_and_global_omit_empty()
    {
        var options = new PimlSerializerOptions { KeyNamingPolicy = PimlNaming.SnakeCase, OmitEmptyValues = true };
        var text = Piml.Serialize(new Config { SiteName = "x", Port = 0, LastUpdated = default }, options);
        Assert.StartsWith("(site_name) x\n", text);
        Assert.DoesNotContain("(port)", text);
        Assert.DoesNotContain("(is_production)", text);
        Assert.DoesNotContain("(admins)", text);
        Assert.DoesNotContain("(ratio)", text);
        Assert.Contains("(last_updated) 0001-01-01T00:00:00\n", text);
    }

    [Fact]
    public void Dictionaries_and_anonymous_objects_serialize()
    {
        var node = PimlSerializer.ToNode(new { Name = "a", Tags = new[] { 1, 2 }, Meta = new Dictionary<string, object?> { ["k"] = null } });
        Assert.Equal("(name) a\n(tags)\n  > 1\n  > 2\n(meta)\n  (k) nil\n", Piml.Write((PimlObject)node));
    }

    [Fact]
    public void Root_must_be_an_object()
    {
        Assert.Throws<PimlException>(() => Piml.Serialize(new[] { 1, 2 }));
        Assert.Throws<PimlException>(() => Piml.Serialize("scalar"));
    }

    [Fact]
    public void Non_string_dictionary_keys_are_rejected()
    {
        Assert.Throws<PimlException>(() => PimlSerializer.ToNode(new Dictionary<int, string> { [1] = "a" }));
    }

    [Theory]
    [InlineData("SiteName", "siteName")]
    [InlineData("URL", "url")]
    [InlineData("HTTPServer", "httpServer")]
    [InlineData("id", "id")]
    public void CamelCase_policy(string input, string expected) => Assert.Equal(expected, PimlNaming.CamelCase(input));

    [Theory]
    [InlineData("SiteName", "site_name")]
    [InlineData("URL", "url")]
    [InlineData("HTTPServer", "http_server")]
    [InlineData("id", "id")]
    public void SnakeCase_policy(string input, string expected) => Assert.Equal(expected, PimlNaming.SnakeCase(input));
}
