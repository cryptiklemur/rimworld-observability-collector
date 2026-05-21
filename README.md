# RimWorld Observability Collector

A telemetry framework for RimWorld mods. Mod authors register named
sections and metrics; an out-of-process collector aggregates the data,
serves a dashboard SPA, and can export diagnostic bundles.

The full specification lives in
`~/Downloads/rimworld_observability_collector_prd.md` (PRD v0.3.0).
Every resolved architectural decision is indexed in PRD §35 (Q1-Q72).

## Three runtimes

The repo deliberately spans three .NET targets because each piece runs
in a different host.

| Project              | Target          | Where it runs                                                       |
| -------------------- | --------------- | ------------------------------------------------------------------- |
| `RimObs.Library/`    | net48           | Inside RimWorld's Unity Mono. Patches game code via Harmony.        |
| `RimObs.Wire/`       | netstandard2.0  | Shared MessagePack types. Linked from both Library and Collector.   |
| `RimObs.Collector/`  | net10.0         | Standalone daemon + CLI. Single self-contained binary per RID.      |
| `RimObs.Dashboard/`  | Svelte 5 + Vite | Static SPA. Built once, embedded as resource in `Collector.exe`.    |

Test projects (`RimObs.Library.Tests/`, `RimObs.Collector.Tests/`) are
`net8.0` xUnit hosts; they exercise the library logic that is
RimWorld-independent.

## Root layout

The non-obvious neighbors at the repo root exist because this is both a
RimWorld mod and a multi-project .NET solution:

- `About/` — RimWorld mod metadata (About.xml, Preview.png, loadFolders.xml).
- `Assemblies/` — RimWorld's deploy directory. `RimObs.Library` builds straight here.
- `PublishedFileIds.json` — Steam Workshop IDs (one per release channel).
- `RimObs.sln` — single solution so Rider/VS resolve `RimObs.Wire` from both net48 and net10.0 consumers.
- `Makefile` + `make.ps1` — see `make build`, `make test`, `make publish-collector`.

## Quick start

```bash
make build              # SPA + full solution
make test               # xUnit suites
make watch              # collector hot-reload at :17654
make publish-collector  # self-contained binaries for win/linux/osx
```

## Conventions

- Hot-path discipline for `RimObs.Library` is mandatory (see PRD §11.6 and
  `.claude/rules/hot-path.md`): zero allocation, exception-safe, no locks,
  no Task/async on the steady path.
- Section names are auto-prefixed with the mod's `packageId` at
  registration (PRD §35.69). Mod authors register bare names like `tick`.
- Wire protocol is MessagePack with per-batch `schema_version`
  (PRD §35.40, §35.42). Default port `17654` for both HTTP and UDP.
- Time source is `Stopwatch.GetTimestamp()` with a session-start UTC
  anchor captured once (PRD §35.67).
