using System.Text.Json;
using Xunit;

namespace Piml.Tests;

public class ComplianceTests
{
    public sealed record Case(string Name, string Piml, JsonElement Json, string? Note);

    static readonly Lazy<(List<Case> Tests, List<Case> Errors)> Suite = new(Load);

    static (List<Case>, List<Case>) Load()
    {
        var path = Environment.GetEnvironmentVariable("PIML_COMPLIANCE");
        if (string.IsNullOrEmpty(path))
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && string.IsNullOrEmpty(path))
            {
                var candidate = Path.GetFullPath(Path.Combine(dir.FullName, "..", "piml", "tests", "compliance.json"));
                if (File.Exists(candidate)) path = candidate;
                dir = dir.Parent;
            }
        }
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new FileNotFoundException("Compliance suite not found. Check out fezcode/piml next to piml.net or set PIML_COMPLIANCE to compliance.json.");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        List<Case> Read(string prop) =>
            root.TryGetProperty(prop, out var arr)
                ? arr.EnumerateArray().Select(e => new Case(
                    e.GetProperty("name").GetString()!,
                    e.GetProperty("piml").GetString()!,
                    e.TryGetProperty("json", out var j) ? j.Clone() : default,
                    e.TryGetProperty("note", out var n) ? n.GetString() : null)).ToList()
                : new List<Case>();
        return (Read("tests"), Read("errors"));
    }

    public static IEnumerable<object[]> ParseCaseNames() => Suite.Value.Tests.Select(c => new object[] { c.Name });
    public static IEnumerable<object[]> ErrorCaseNames() => Suite.Value.Errors.Select(c => new object[] { c.Name });

    static Case Find(IEnumerable<Case> cases, string name) => cases.First(c => c.Name == name);

    [Fact]
    public void Suite_declares_the_implemented_spec_version_and_is_not_empty()
    {
        Assert.NotEmpty(Suite.Value.Tests);
        Assert.NotEmpty(Suite.Value.Errors);
    }

    [Theory]
    [MemberData(nameof(ParseCaseNames))]
    public void Parses_like_the_reference(string name)
    {
        var tc = Find(Suite.Value.Tests, name);
        var expected = Normalize(tc.Json);
        var actual = Normalize(Piml.Parse(tc.Piml));
        Assert.True(DeepEqual(expected, actual), $"parse mismatch for '{name}'\nexpected: {Describe(expected)}\nactual:   {Describe(actual)}");
    }

    [Theory]
    [MemberData(nameof(ErrorCaseNames))]
    public void Rejects_invalid_documents(string name)
    {
        var tc = Find(Suite.Value.Errors, name);
        Assert.Throws<PimlSyntaxException>(() => Piml.Parse(tc.Piml));
    }

    [Theory]
    [MemberData(nameof(ParseCaseNames))]
    public void Round_trips_through_the_writer(string name)
    {
        var tc = Find(Suite.Value.Tests, name);
        var expected = Normalize(tc.Json);
        var written = Piml.Write(Piml.Parse(tc.Piml));
        var again = Normalize(Piml.Parse(written));
        Assert.True(DeepEqual(expected, again), $"round-trip mismatch for '{name}'\nwritten:\n{written}\nexpected: {Describe(expected)}\nactual:   {Describe(again)}");
    }

    // ---- normalization: both sides become null/bool/long/double/string/List/Dictionary ----

    public static object? Normalize(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var l) && !e.GetRawText().Contains('.') ? (object)l : e.GetDouble(),
        JsonValueKind.Array => e.EnumerateArray().Select(Normalize).ToList(),
        JsonValueKind.Object => e.EnumerateObject().ToDictionary(p => p.Name, p => Normalize(p.Value), StringComparer.Ordinal),
        _ => throw new InvalidOperationException(e.ValueKind.ToString())
    };

    public static object? Normalize(PimlNode n) => n switch
    {
        PimlNull => null,
        PimlBoolean b => b.Value,
        PimlInteger i => i.Value,
        PimlFloat f => f.Value,
        PimlString s => s.Value,
        PimlArray a => a.Select(Normalize).ToList(),
        PimlObject o => o.ToDictionary(kv => kv.Key, kv => Normalize(kv.Value), StringComparer.Ordinal),
        _ => throw new InvalidOperationException(n.Kind.ToString())
    };

    public static bool DeepEqual(object? a, object? b)
    {
        switch (a)
        {
            case null: return b == null;
            case Dictionary<string, object?> da:
                return b is Dictionary<string, object?> db && da.Count == db.Count
                       && da.All(kv => db.TryGetValue(kv.Key, out var v) && DeepEqual(kv.Value, v));
            case List<object?> la:
                return b is List<object?> lb && la.Count == lb.Count && la.Zip(lb, DeepEqual).All(x => x);
            default:
                return a.Equals(b);
        }
    }

    static string Describe(object? v) => v switch
    {
        null => "null",
        string s => "\"" + s.Replace("\n", "\\n") + "\"",
        Dictionary<string, object?> d => "{" + string.Join(", ", d.Select(kv => kv.Key + ": " + Describe(kv.Value))) + "}",
        List<object?> l => "[" + string.Join(", ", l.Select(Describe)) + "]",
        _ => v.ToString() + " (" + v.GetType().Name + ")"
    };
}
