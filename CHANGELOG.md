# Changelog

## 1.0.0 — 2026-09-05

- Initial release implementing PIML spec v1.2.0.
- Parser with `PimlSyntaxException` positions; ordered `PimlNode` tree.
- Canonical writer with go-piml-compatible quoting and escaping.
- Typed serializer (`Piml.Serialize` / `Piml.Deserialize`) with attributes and naming policies.
- Lossless `PimlDocument` editing (`Set` / `Remove`).
- Passes the shared compliance suite (59 parse cases, 13 error cases, 59 round-trips).
