# Completion Gate G2 — Icod.CommandFramework extraction

This document records the retained standalone ownership boundary for `Icod.CommandFramework` after the original extraction from `Icod.CoreUtils`.

Beginning with 2.0.0, one-time compatibility material and migration-only scaffolding are no longer part of the repository. The current tree contains only infrastructure that remains owned and maintained by `Icod.CommandFramework`.

## Standalone repository shape

```text
Icod.CommandFramework/
├── Icod.CommandFramework.csproj
├── Icod.CommandFramework.sln
├── LICENSE
├── README.md
├── icon.png
├── src/
└── tests/
    └── CommandFramework.Tests/
        └── Icod.CommandFramework.Tests.csproj
```

The root project deliberately disables default compile items and explicitly compiles `src/**/*.cs`; this allows the package project to live beside the solution while `tests/` remains beneath the repository root.

## Current production ownership boundary

The following source areas remain complete command-neutral framework areas:

- `CommandLine`
- `Delimiters`
- `Diagnostics`
- `IO`
- `Records`
- `RegularExpressions`
- `Temporary`
- `Text`

The following lower-level areas are also retained:

- `Platform`: cross-platform capability/result contracts, identity services, security-context support, and SELinux integration.
- `FileSystem`: capability and operation contracts together with `Metadata`, `Modes`, `Mutation`, `RecursiveMutation`, `Traversal`, and `TransactionalReplacement`.

These areas belong in the framework because they provide reusable mechanism rather than command-family policy.

## Ownership rule

Command-family and suite-specific policy remains outside `Icod.CommandFramework`. A feature belongs here only when its semantics are command-neutral and independently reusable by unrelated consumers.

That boundary is intentionally stricter in 2.0.0 than it was during the original extraction. Historical migration convenience is no longer a reason to retain an API.

## Tests

`Icod.CommandFramework.Tests` contains tests only for production APIs that remain owned by this repository. Tests under `src` mirror the production subsystem whose behavior they exercise.

The test project references the package project directly and is not packed.

## Namespace and assembly migration

The original extraction normalized the shared source and test namespaces:

- `Icod.CoreUtils.Shared` → `Icod.CommandFramework`
- `Icod.CoreUtils.Shared.Tests` → `Icod.CommandFramework.Tests`
- corresponding `.dll` assembly-name strings

Current production source must not depend on suite-specific libraries such as `Icod.DiffUtils`, `Icod.ProcPs`, or `Icod.LineEditor`.

## Package metadata

The package follows the established `Icod.Path` conventions:

- `net10.0`, C# 13
- `2.0.0`
- LGPL-3.0-or-later
- deterministic builds
- XML documentation
- symbol package (`snupkg`)
- package README and icon
- repository publishing metadata
- dependency on published `Icod.Path` `1.0.0`

The package/repository description is:

> Cross-platform .NET command infrastructure for argument parsing, diagnostics, byte-oriented streaming, filesystem operations, records, regular expressions, temporary storage, and text.

## Current development workflow

The one-time importer used to create the standalone repository is no longer part of the current tree. Development now occurs directly in this repository.

Validate changes with the normal solution workflow:

```sh
dotnet restore Icod.CommandFramework.sln
dotnet build Icod.CommandFramework.sln -c Release
dotnet test Icod.CommandFramework.sln -c Release
```
