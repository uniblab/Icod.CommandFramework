# Completion Gate G2 — Icod.CommandFramework extraction

This repository overlay implements the standalone shape for `Icod.CommandFramework` and records the source/test ownership boundary used for the extraction from `Icod.CoreUtils`.

## Standalone repository shape

```text
Icod.CommandFramework/
├── Icod.CommandFramework.csproj
├── Icod.CommandFramework.sln
├── LICENSE
├── README.md
├── icon.png
├── src/
├── tests/
│   ├── CommandFramework.Tests/
│   │   └── Icod.CommandFramework.Tests.csproj
│   └── ProcessTestHost/
│       └── Icod.CommandFramework.ProcessTestHost.csproj
└── tools/
    ├── framework-source-manifest.txt
    ├── framework-test-manifest.txt
    ├── HISTORY-IMPORT.md
    └── migrate-from-coreutils.ps1
```

The root project deliberately disables default compile items and explicitly compiles `src/**/*.cs`; this allows the single package project to live beside the solution while `tests/` remains beneath the repository root.

## G2 production ownership boundary

The following source areas move as complete command-neutral areas:

- `CommandLine`
- `Delimiters`
- `Diagnostics`
- `Host`
- `IO`
- `Processes`
- `Records`
- `RegularExpressions`
- `Temporary`
- `Terminal`

The following areas are split more narrowly:

- `Text`: all current text primitives except `TabStop*.cs`.
- `Time`: `MonotonicClock.cs` and `PeriodicScheduler.cs`.
- `Platform`: `PlatformCapabilities.cs`, `PlatformFeature.cs`, and `PlatformOperationResult.cs`.
- `FileSystem`: the root capability/operation contracts plus `Metadata`, `Mutation`, `RecursiveMutation`, `Traversal`, and `TransactionalReplacement`.
- `FileSystem/Modes`: only `PosixFileMode.cs`, because the mutation/transaction layers need the neutral POSIX mode vocabulary. The GNU mode-expression/parser policy remains in CoreUtils.

`RecursiveMutation` is part of the framework boundary because it is infrastructure used by the transactional replacement layer and its design keeps command-specific recursion/overwrite policy with callers.

## Deliberately retained in Icod.CoreUtils.Shared

At this gate, the extraction does **not** move command-family or suite-specific policy merely because it currently resides in `Shared`. In particular, the importer leaves behind:

- `BinaryFormatting`
- `Checksums`
- `Codecs`
- `DirectoryListing`
- `Escapes`
- `Formatting`
- `Numerics`
- `Ordering`
- `Ranges`
- `SharedUtils`
- GNU date/time parsing and formatting helpers not represented by the two neutral scheduling primitives above
- platform helpers outside the capability surface selected above
- `FileSystem/CopyMove`
- `FileSystem/Ownership`
- `FileSystem/Usage`
- GNU mode expressions/parser and the system creation-mask provider in `FileSystem/Modes`
- `Text/TabStop*`

These can be revisited only when an independent suite consumer demonstrates that they belong below the suite layer.

## Tests

The old `Shared.Tests` project is split rather than copied wholesale. Tests move when the production API they exercise moves. The resulting test assembly is `Icod.CommandFramework.Tests`.

The repository also contains `Icod.CommandFramework.ProcessTestHost`, a small executable used by process-runner tests. It is test infrastructure, is not packed, and does not become part of the public package API.

The exact selected paths are recorded in `tools/framework-test-manifest.txt`.

## Namespace and assembly migration

The importer rewrites:

- `Icod.CoreUtils.Shared` → `Icod.CommandFramework`
- `Icod.CoreUtils.Shared.Tests` → `Icod.CommandFramework.Tests`
- `Icod.CoreUtils.ProcessTestHost` → `Icod.CommandFramework.ProcessTestHost`
- corresponding `.dll` assembly-name strings

It then fails if migrated C# still contains `Icod.CoreUtils`, or if framework production source contains a dependency on `Icod.DiffUtils`, `Icod.ProcPs`, or `Icod.LineEditor`.

## Package metadata

The package follows the established `Icod.Path` conventions:

- `net10.0`, C# 13
- `1.0.0-WIP1` prerelease starting point
- LGPL-3.0-or-later
- repository metadata
- deterministic builds
- XML documentation
- symbol package (`snupkg`)
- package README and icon
- SourceLink-compatible repository publishing metadata
- dependency on published `Icod.Path` `1.0.0`

The package/repository description is:

> Cross-platform .NET command infrastructure for argument parsing, diagnostics, streams, filesystem, processes, terminals, temporary storage, and text.

## Running the extraction

From the `Icod.CommandFramework` checkout, with a sibling or otherwise available `Icod.CoreUtils` checkout:

```powershell
./tools/migrate-from-coreutils.ps1 -CoreUtilsRoot ../Icod.CoreUtils -Validate
```

`-Validate` restores once, builds and tests Debug/Staging/Release, and packs the Release package into `artifacts/`.

After a successful standalone build, update `Icod.CoreUtils` to replace the project reference with the published/pre-release package, then run its complete supported-OS CI matrix before declaring G2 complete.
