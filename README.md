# Icod.CommandFramework

Cross-platform .NET command infrastructure for argument parsing, diagnostics, streams, filesystem, processes, terminals, temporary storage, and text.

`Icod.CommandFramework` is the neutral command-suite foundation extracted from the shared infrastructure developed in `Icod.CoreUtils`. It provides reusable contracts and implementations used by independent command families without making one command suite depend on another.

The package targets .NET 10 and C# 13. Canonical-path behavior remains in the separate `Icod.Path` package.

## Repository layout

- `Icod.CommandFramework.csproj` — package project
- `src/` — framework source
- `tests/CommandFramework.Tests/` — framework tests
- `tests/ProcessTestHost/` — repository-local child-process test fixture

## License

Licensed under LGPL-3.0-or-later. See `LICENSE`.
