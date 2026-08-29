# Icod.CommandFramework

**Cross-platform .NET infrastructure for building serious command-line tools.**

`Icod.CommandFramework` provides the reusable machinery that tends to sit underneath Unix-style command-line programs: option parsing, deterministic diagnostics, byte-oriented streaming, record processing, filesystem capabilities and mutation, secure temporary objects, text/display-width handling, and managed GNU/POSIX regular expressions.

It is intended for applications that need more than a thin wrapper around `System.Console` and `System.IO`—especially tools that must behave predictably across Windows, Linux, and macOS while preserving Unix/POSIX semantics where those semantics matter.

The library originated as the command-neutral infrastructure developed while porting GNU Coreutils and related Unix utilities to C#. It is now a standalone package so unrelated command suites and applications can reuse the same tested foundation without depending on `Icod.CoreUtils`.

> [!NOTE]
> **2.1.0 adds canonical pathname globbing.** `FileSystem.Traversal.PathnameExpander`
> now provides the authoritative expansion engine, including `*`, `?`, recursive
> `**`, character classes, deterministic ordering, structured expansion issues,
> and command-oriented `ExpandOperandsAsync`. Pathname grammar comes from
> `Icod.Path 1.1.0`, while the synchronous `IO.PathnameExpander` remains available
> as a compatibility facade.
>
> [!IMPORTANT]
> **2.0.0 was a breaking release.** Deprecated compatibility surfaces maintained
> only for migration were removed. The package contains only APIs owned by the
> command-neutral framework itself.

## Why use it?

.NET already has excellent general-purpose APIs, but command-line utilities often need contracts that the BCL intentionally does not provide as a single coherent layer.

A nontrivial CLI may need to answer questions such as:

- Is this option a clustered short option, an abbreviated long option, an operand, or a syntax error?
- Can I process an arbitrarily large NUL-delimited record without buffering the whole record?
- Can I preserve malformed input bytes while still making Unicode-aware width or matching decisions?
- Does this platform support the filesystem operation I need, or only an approximation?
- How do I implement GNU/POSIX BRE or ERE semantics when `System.Text.RegularExpressions` intentionally implements a different regular-expression language and matching model?
- How do I create a temporary file or directory without introducing a predictable-name race?

`Icod.CommandFramework` exists to make those problems reusable infrastructure rather than something every command has to solve again.

### The design priorities are deliberate

**Deterministic behavior.** Expected syntax, platform, and I/O failures are represented through stable results and diagnostics instead of being left to incidental exception text.

**Byte fidelity.** Many Unix tools operate on bytes and records, not merely .NET strings. The framework contains byte-preserving readers, record models, delimiters, and matching surfaces for programs that cannot silently normalize their input.

**Bounded streaming.** Large files, pipes, and records should not require whole-input buffering. Streaming and segmented APIs are used where input size may be unbounded.

**Cross-platform honesty.** Windows, Linux, and macOS do not expose identical filesystem, identity, security-context, and locale semantics. The framework prefers explicit capability and result models over pretending that unlike operations are identical.

**Testability.** Streams, filesystem services, locale behavior, and similar environmental boundaries are exposed through reusable contracts so command logic can be tested without depending on global console or environment state.

**Mechanism, not command policy.** The framework provides reusable primitives. Individual commands still own their option combinations, command-specific grammar, help text, presentation, business rules, and compatibility policy.

## What is included?

The package is organized into focused namespaces. Most areas also contain a local `README.md` with deeper design notes and invariants.

| Namespace | Purpose |
| --- | --- |
| `Icod.CommandFramework.CommandLine` | GNU/POSIX-style option parsing: short options, clustered short options, long options and abbreviations, option values, operand ordering, source positions, rewrite rules, and structured parse errors. |
| `Icod.CommandFramework.Diagnostics` | Command execution context, standard streams, cancellation, command results, exit-code conventions, and consistent program-name-prefixed diagnostics. |
| `Icod.CommandFramework.Delimiters` | Byte delimiters and separators, repeating separator cycles, and incremental multibyte delimiter matching across buffer boundaries. |
| `Icod.CommandFramework.IO` | Input operands and the conventional `-` standard-input marker, byte/token readers, delimited readers and writers, bounded stream operations, the legacy synchronous pathname-expansion facade, text/byte adaptation, and temporary spooling. |
| `Icod.CommandFramework.Records` | Byte-preserving record models and readers/writers, including segmented processing for records too large to materialize as one buffer. |
| `Icod.CommandFramework.FileSystem` | Filesystem capability discovery, metadata, canonical pathname-pattern expansion and traversal, POSIX mode vocabulary, mutation, recursive mutation, and transactional replacement through reusable system/provider boundaries. |
| `Icod.CommandFramework.Platform` | Cross-platform feature/result contracts, user/group and current-identity services, security-context capability checks, and SELinux integration where available. |
| `Icod.CommandFramework.Text` | Byte-preserving text units and logical lines, malformed-encoding policy, locale classification, Unicode display width, display-column tracking, and explicit/recurring tab-stop models. |
| `Icod.CommandFramework.RegularExpressions` | Fully managed GNU Basic, GNU Extended, and GNU Emacs regular-expression profiles with POSIX/GNU leftmost-longest matching, captures, locale-aware character classes, cancellation/resource limits, and exact byte-coordinate matching. |
| `Icod.CommandFramework.Temporary` | Cryptographically strong temporary-name generation, exclusive file/directory creation, template handling, collision reporting, and disposable temporary workspaces. |

### Command-line parsing

`Icod.CommandFramework.CommandLine` handles reusable command-line **syntax** rather than the semantics of a particular program. It supports short and long options, clustered short options, abbreviations, required and optional values, configurable operand ordering, deterministic parse errors, and narrow compatibility rewrites.

The important boundary is that the parser does not decide what a command *means*. A command remains responsible for semantic validation such as mutually exclusive options, required operand counts, numeric ranges, or command-specific subgrammars.

See [`src/CommandLine/README.md`](src/CommandLine/README.md).

### Streaming, delimiters, and records

Unix pipelines frequently make byte identity observable. Newline is not always the record separator, NUL-delimited data is common, malformed bytes may need to survive unchanged, and a record may be much larger than a reasonable in-memory buffer.

The `IO`, `Delimiters`, and `Records` namespaces therefore provide both convenient whole-record operations and bounded segmented operations. Delimiters can span arbitrary input-buffer boundaries, record termination is modeled explicitly, and callers retain control over whether separators are preserved, omitted, replaced, or synthesized.

See [`src/IO/README.md`](src/IO/README.md), [`src/Delimiters/README.md`](src/Delimiters/README.md), and [`src/Records/README.md`](src/Records/README.md).

### Filesystem infrastructure

Portable command-line tools need more than `File.Copy` and `Directory.EnumerateFiles`. The framework provides capability discovery and lower-level contracts for metadata, traversal, mutation, recursive mutation, and transactional replacement.

The intent is to separate **what the platform can do** from **what a command chooses to do**. A copy, remove, archive, or install command can build its own policy on top of the common mechanics rather than embedding that policy in the framework.

Canonical-path resolution and pathname grammar are intentionally maintained in the separate `Icod.Path` package and are referenced by `Icod.CommandFramework`.

#### Pathname patterns and glob expansion

The canonical globbing API is `Icod.CommandFramework.FileSystem.Traversal.PathnameExpander`. `PathnamePattern` supplies matching without filesystem enumeration, `ExpandAsync` exposes provenance-preserving expansion events, and `ExpandOperandsAsync` supplies the ordered pathname collection most command implementations need.

The canonical pattern grammar is segment-aware:

- `*` matches zero or more characters within one pathname segment.
- `?` matches exactly one character within one pathname segment.
- `**` is recursive only when the complete segment is exactly `**`; it matches zero or more pathname segments.
- `[abc]`, `[a-z]`, `[!a-z]`, and `[^a-z]` are character classes in the canonical traversal API.
- An unterminated or otherwise unparseable character class is treated as literal text rather than guessed into another pattern.
- By default, wildcard tokens do not match a leading `.` unless the segment explicitly begins with a literal period. `LeadingPeriodPolicy` can opt into wildcard matching.
- Host pathname case rules are used by default: ordinal case-insensitive on Windows and ordinal case-sensitive elsewhere.
- On Unix-like hosts, backslash can quote a following metacharacter by default. On Windows it is a pathname separator.

Path roots, separators, volume identity, and component decomposition come from `Icod.Path`; wildcard meaning, matching, filesystem enumeration, recursive traversal, ordering, link policy, and unmatched-pattern policy remain owned by `Icod.CommandFramework`.

Expansion defaults are intentionally conservative and deterministic. Unmatched patterns are preserved as literal operands, wildcard-discovered directory links are not followed, filesystem boundaries may be crossed unless disabled, and matches are ordered deterministically according to host pathname comparison. `PathnameExpansionOptions` exposes the corresponding policies explicitly, including provider-order opt-out, depth and per-directory resource limits, error continuation, and filesystem boundaries.

`ExpandOperandsAsync` retains literal operand spellings even when file/directory filtering is enabled. This allows command code to preserve conventional operands such as `-` and to decide for itself whether a literal pathname must exist. Non-root expansion events remain available through `PathnameOperandExpansionResult.Issues`.

`Icod.CommandFramework.IO.PathnameExpander` remains as a synchronous compatibility facade for existing consumers. Its intentionally narrower historical grammar recognizes `*`, `?`, and segment-level `**`; bracket expressions remain literal through that legacy surface. New pathname-expansion code should prefer `FileSystem.Traversal`.

See [`src/FileSystem/Traversal/README.md`](src/FileSystem/Traversal/README.md).

### Text and display width

Console text is not the same thing as a sequence of .NET `char` values.

The text layer can retain authoritative source bytes while decoding only as needed for classification and width decisions. It provides explicit malformed-input policies, C/POSIX and Unicode locale behavior, Unicode display-width calculation, logical-line handling, checked display-column state, and reusable tab-stop models.

This is useful for formatting, columnar output, folding, alignment, and other tools where the number of display columns matters independently of string length.

See [`src/Text/README.md`](src/Text/README.md).

### GNU/POSIX regular expressions

The regular-expression layer is intentionally **not** a source-to-source translation into `System.Text.RegularExpressions`.

GNU Basic Regular Expressions, Extended Regular Expressions, and the GNU Emacs profile have syntax and matching rules that differ materially from .NET regexes. In particular, POSIX/GNU leftmost-longest selection, capture behavior, locale-aware bracket classes, byte-oriented matching, malformed-input handling, and GNU compatibility rules cannot be reproduced reliably by changing a few metacharacters and handing the result to `Regex`.

`Icod.CommandFramework.RegularExpressions` therefore contains a managed parser and matcher with explicit GNU/POSIX profiles. It can match .NET strings using UTF-16 coordinates or authoritative byte input using exact source-byte offsets.

See [`src/RegularExpressions/README.md`](src/RegularExpressions/README.md).

### Secure temporary objects

Temporary-name generation is security-sensitive when a program creates files on behalf of a user or operates in shared directories.

The temporary-object layer uses cryptographically secure random substitutions and exclusive creation semantics. Existing files, directories, or links are treated as collisions rather than targets to follow or replace. The API can create both individual temporary objects and disposable workspaces.

See [`src/Temporary/README.md`](src/Temporary/README.md).

## What this library is not

`Icod.CommandFramework` is intentionally a low-level command infrastructure library.

It is **not**:

- a command-dispatch or dependency-injection framework;
- a shell, shell parser, or external-command scripting language;
- a replacement for command-specific option validation or help generation;
- an implementation of GNU Coreutils;
- a license to delegate work to installed native utilities;
- a promise that every Unix concept can be emulated perfectly on every operating system.

The library supplies mechanisms that can be shared safely. Programs built on it remain responsible for their own externally visible contract.

## Who is it for?

`Icod.CommandFramework` is most useful when you are building:

- cross-platform command-line utilities;
- Unix/POSIX-compatible tools in managed code;
- file, text, archive, and data-processing utilities;
- applications that consume large streams or byte-delimited data;
- testable CLI engines that should not read directly from global console/environment state;
- tools that need explicit platform capability reporting rather than optimistic emulation;
- software that needs GNU/POSIX regular-expression behavior rather than .NET regex behavior.

For a small application that only needs a few switches and writes ordinary text to `Console.Out`, a smaller CLI package may be a better fit. This framework earns its keep when platform behavior, byte fidelity, streaming, or Unix compatibility become part of the program's contract.

## Platform and target framework

The package targets:

- **.NET 10**
- **C# 13**

The repository CI builds and tests on:

- Windows
- Ubuntu Linux
- macOS

Some capabilities are naturally platform-specific. For example, SELinux support requires Linux and a usable `libselinux`, while filesystem-metadata, identity, security-context, and locale behavior varies by operating system. Platform-specific features are exposed through capability/provider contracts so consumers can handle those differences explicitly.

## Package dependency

`Icod.CommandFramework` depends on:

- `Icod.Path 1.1.0` — canonical-path services plus platform-aware pathname grammar and decomposition used by the globbing layer.

## Installation

When consuming a published package:

```sh
dotnet add package Icod.CommandFramework
```

For a prerelease build:

```sh
dotnet add package Icod.CommandFramework --prerelease
```

Then import only the namespaces needed by the command or library you are building.

## Design philosophy

The framework follows a few rules that are especially important for command-line software:

1. **Do not hide observable behavior.** Byte offsets, record termination, platform support, and approximation are often part of a command's public contract.
2. **Do not turn every expected failure into an exception.** Ordinary races, unsupported operations, and parse failures should be representable as controlled data.
3. **Do not make commands depend on one another.** Reusable infrastructure belongs below command suites; suite-specific policy belongs above it.
4. **Do not shell out to solve portability.** Native APIs may be used when necessary, but the framework is not implemented by invoking the operating system's native copy of the command being reimplemented.
5. **Keep environmental dependencies injectable.** Doing so makes both portability and testing substantially easier.
6. **Prefer bounded algorithms for unbounded input.** Pipes, files, and records can be arbitrarily large.
7. **Be explicit when semantics differ by platform.** A controlled `Unsupported` result is often safer than a misleading approximation.

These rules grew out of implementing real command suites whose behavior is observable in scripts, pipelines, CI systems, and compatibility tests.

## Repository layout

```text
Icod.CommandFramework.csproj
Icod.CommandFramework.sln
src/
    CommandLine/
    Delimiters/
    Diagnostics/
    FileSystem/
    IO/
    Platform/
    Records/
    RegularExpressions/
    Temporary/
    Text/
tests/
    CommandFramework.Tests/
```

## Building and testing

```sh
dotnet restore Icod.CommandFramework.sln
dotnet build Icod.CommandFramework.sln -c Debug
dotnet test Icod.CommandFramework.sln -c Debug
```

Release builds treat compiler warnings as errors except for missing XML documentation warnings.

## Project origins

The framework was extracted from the shared infrastructure developed for `Icod.CoreUtils`, a managed C# implementation of GNU Coreutils plus related Unix utilities. During that work, functionality used by multiple command families was deliberately separated from command-specific engines.

That history matters because these APIs were not designed only from hypothetical abstractions: they were exercised against concrete requirements involving GNU-style option parsing, byte-oriented pipelines, filesystem edge cases, regular-expression compatibility, and cross-platform tests.

The standalone package exists so that infrastructure can now be used independently.

## License

Licensed under **LGPL-3.0-or-later**. See [`LICENSE`](LICENSE).
