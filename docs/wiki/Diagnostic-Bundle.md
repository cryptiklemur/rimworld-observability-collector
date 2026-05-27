# Diagnostic bundle

A zip archive that captures a session's data, configuration, and log excerpts into a single file for bug reports and post-mortem analysis.

## Status: planned

The diagnostic bundle is not yet implemented. It is tracked as a Phase 4 deliverable.

The `export-bundle` subcommand is registered in the CLI but returns an error when invoked:

```
Collector export-bundle <session-id>
# exits with code 3: "export-bundle is not yet implemented."
```

No HTTP endpoint for bundle export exists yet.

The intended format and contents are specified in PRD §15. When the feature ships, this page will be expanded to cover the zip layout, manifest schema, privacy considerations, and consumption instructions.

## Related

- [Local HTTP API](Local-HTTP-API)
- [Collector CLI](Collector-CLI)
- [Wire protocol](Wire-Protocol)
- [Troubleshooting](Troubleshooting)
