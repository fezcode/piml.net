# piml.net

`piml.net` is the official .NET library for **PIML** (Parenthesis Intended Markup Language): a parser, a canonical writer, a typed serializer, and a lossless document editor.

Implements **PIML spec v1.2.0** — see the [spec repository](https://github.com/fezcode/piml). Conformance is verified against the shared compliance suite (`piml/tests/compliance.json`).

- Package: [`Piml` on NuGet](https://www.nuget.org/packages/Piml)
- Targets: `net10.0`, `netstandard2.0`
- Dependencies: none

## Features

- **Ordered value tree** — `PimlObject` keeps key order; `PimlArray`, `PimlString`, `PimlInteger`, `PimlFloat`, `PimlBoolean`, `PimlNull`.
- **Spec-exact parsing** — 2-space indentation, `nil`, quoted strings, inline comments, `\#` escapes, multi-line blocks, labeled list items; every violation throws `PimlSyntaxException` with line and column.
- **Canonical writer** — quotes only when a value would not parse back unchanged; escapes multi-line content; writes empty collections as `nil`.
- **Typed serialization** — `Piml.Serialize<T>` / `Piml.Deserialize<T>` over public properties and fields, `[PimlKey]`, `[PimlIgnore]`, `[PimlOmitEmpty]`, camelCase / snake_case naming policies, records, enums, dates (RFC 3339), collections and dictionaries, schema-aware coercion (`"8080"` → `int`).
- **Lossless editing** — `Piml.ParseDocument` returns a `PimlDocument` whose `Set` / `Remove` rewrite only the affected lines: comments, blank lines, key order, quoting and CRLF survive.

## Install

```
dotnet add package Piml
```

## Usage

### Parse

```csharp
using Piml;

var root = Piml.Parse("""
(site_name) PIML Demo
(port) 8080
(features)
  > auth
  > logging
(description)
  Multi-line
  text.
""");

long port = ((PimlInteger)root["port"]).Value;          // 8080
var features = (PimlArray)root["features"];             // ["auth", "logging"]
```

### Typed

```csharp
public sealed class Config
{
    public string SiteName { get; set; } = "";
    public int Port { get; set; }
    public List<string> Features { get; set; } = new();
    [PimlKey("last_updated")] public DateTime LastUpdated { get; set; }
    [PimlOmitEmpty] public string Notes { get; set; } = "";
}

var cfg = Piml.Deserialize<Config>(text)!;
string back = Piml.Serialize(cfg);                       // keys are camelCase by default
string snake = Piml.Serialize(cfg, new PimlSerializerOptions { KeyNamingPolicy = PimlNaming.SnakeCase });
```

### Edit without losing comments

```csharp
var doc = Piml.ParseDocument(File.ReadAllText("settings.piml"));
doc.Set(new[] { "editor", "fontSize" }, 16);            // rewrites one line, keeps "# px"
doc.Set("theme", "studio-fluent");                       // appends at the end
doc.Remove("editor", "rulers");
File.WriteAllText("settings.piml", doc.ToString());
```

### Write

```csharp
var obj = new PimlObject { { "name", "fez" }, { "tags", new PimlArray { "editor", "ide" } } };
string text = Piml.Write(obj);
```

## Type inference

Unquoted values become `nil` → null, `true`/`false` → bool, `^-?(0|[1-9][0-9]*)$` → long, `^-?(0|[1-9][0-9]*)\.[0-9]+$` → double, anything else → string. Quote a value (`"08080"`) to force a string. A `nil` (or bare key) is null, an empty list, or an empty object depending on the target type.

## Compliance

`tests/Piml.Tests/ComplianceTests.cs` runs every case in `piml/tests/compliance.json` (parse, error, and round-trip). Check out `fezcode/piml` next to this repo or set `PIML_COMPLIANCE` to the file's path.

## Contributing

Open an issue or a pull request. Run `dotnet test` before submitting.

## License

MIT — see [LICENSE](LICENSE).
