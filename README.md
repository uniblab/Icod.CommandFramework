# Icod.CommandFramework

**Cross-platform .NET infrastructure for building serious command-line tools.**

`Icod.CommandFramework` provides the reusable machinery that tends to sit underneath Unix-style command-line programs: option parsing, deterministic diagnostics, byte-oriented streaming, record processing, filesystem capabilities and mutation, process execution and control, terminal handling, secure temporary objects, text/display-width handling, host observations, and managed GNU/POSIX regular expressions.

It is intended for applications that need more than a thin wrapper around `System.Console`, `System.IO`, and `System.Diagnostics.Process`—especially tools that must behave predictably across Windows, Linux, and macOS while preserving Unix/POSIX semantics where those semantics matter.

The library originated as the command-neutral infrastructure developed while porting GNU Coreutils and related Unix utilities to C#. It is now a standalone package so unrelated command suites and applications can reuse the same tested foundation without depending on `Icod.CoreUtils`.

> [!IMPORTANT]
> The historical `Icod.CommandFramework.Host`, `Icod.CommandFramework.Processes`,
> `Icod.CommandFramework.Terminal`, and `Icod.CommandFramework.Time` APIs are
> compatibility-only and deprecated. New code should use `Icod.Host`,
> `Icod.Processes`, `Icod.Terminal`, and `Icod.Timing` respectively. Terminal
> database and curses functionality is maintained separately in `Icod.TermInfo`
> and `Icod.DCurses`. `ObservationFidelity` remains in CommandFramework because
> it describes consumer semantic fidelity rather than a factual host observation.

## Why use it?

.NET already has excellent general-purpose APIs, but command-line utilities often need contracts that the BCL intentionally does not provide as a single coherent layer.

A nontrivial CLI may need to answer questions such as:

- Is this option a clustered short option, an abbreviated long option, an operand, or a syntax error?
- Can I process an arbitrarily large NUL-delimited record without buffering the whole record?
- Can I preserve malformed input bytes while still making Unicode-aware width or matching decisions?
- Does this platform support the filesystem or process operation I need, or only an approximation?
- How do I launch a child with an exact argument vector, controlled environment, redirected streams, cancellation, timeout, process-group behavior, and deterministic failure reporting?
- Is output attached to a terminal? How wide is it? Does it support color? What terminal-control operations are actually available?
- How do I implement GNU/POSIX BRE or ERE semantics when `System.Text.RegularExpressions` intentionally implements a different regular-expression language and matching model?
- How do I create a temporary file or directory without introducing a predictable-name race?
- How do I distinguish an unavailable host fact from a meaningless value such as zero?

`Icod.CommandFramework` exists to make those problems reusable infrastructure rather than something every command has to solve again.

### The design priorities are deliberate

**Deterministic behavior.** Expected syntax, platform, I/O, and process failures are represented through stable results and diagnostics instead of being left to incidental exception text.

**Byte fidelity.** Many Unix tools operate on bytes and records, not merely .NET strings. The framework contains byte-preserving readers, record models, delimiters, and matching surfaces for programs that cannot silently normalize their input.

**Bounded streaming.** Large files, pipes, and records should not require whole-input buffering. Streaming and segmented APIs are used where input size may be unbounded.

**Cross-platform honesty.** Windows, Linux, and macOS do not expose identical filesystem, process, signal, identity, and terminal semantics. The framework prefers explicit capability and fidelity models over pretending that unlike operations are identical.

**Testability.** Streams, clocks, resource providers, process services, terminal services, filesystem services, locale behavior, and similar environmental boundaries are exposed through reusable contracts so command logic can be tested without depending on process-global state.

**Mechanism, not command policy.** The framework provides reusable primitives. Individual commands still own their option combinations, command-specific grammar, help text, presentation, business rules, and compatibility policy.

## What is included?

The package is organized into focused namespaces. Most areas also contain a local `README.md` with deeper design notes and invariants.

| Namespace | Purpose |
| --- | --- |
| `Icod.CommandFramework.CommandLine` | GNU/POSIX-style option parsing: short options, clustered short options, long options and abbreviations, option values, operand ordering, source positions, rewrite rules, and structured parse errors. |
| `Icod.CommandFramework.Diagnostics` | Command execution context, standard streams, cancellation, command results, exit-code conventions, and consistent program-name-prefixed diagnostics. |
| `Icod.CommandFramework.Delimiters` | Byte delimiters and separators, repeating separator cycles, and incremental multibyte delimiter matching across buffer boundaries. |
| `Icod.CommandFramework.IO` | Input operands and the conventional `-` standard-input marker, byte/token readers, delimited readers and writers, bounded stream operations, pathname expansion, text/byte adaptation, and temporary spooling. |
| `Icod.CommandFramework.Records` | Byte-preserving record models and readers/writers, including segmented processing for records too large to materialize as one buffer. |
| `Icod.CommandFramework.FileSystem` | Filesystem capability discovery, metadata, POSIX mode vocabulary, traversal, mutation, recursive mutation, and transactional replacement through reusable system/provider boundaries. |
| `Icod.CommandFramework.Processes` | Executable lookup, child environments, exact process launch, stream forwarding/capture, cancellation and timeout, process identities and targets, signals, process groups, priority/nice operations, waiting, termination, and controlled operation results. |
| `Icod.CommandFramework.Platform` | Cross-platform feature/result contracts, user/group/process identity services, security-context capability checks, and SELinux integration where the host provides it. |
| `Icod.CommandFramework.Terminal` | Terminal attachment and dimensions, environment observation, color policy, filename presentation, terminal modes, and Unix/Windows terminal-control providers. |
| `Icod.CommandFramework.Text` | Byte-preserving text units and logical lines, malformed-encoding policy, locale classification, Unicode display width, display-column tracking, and explicit/recurring tab-stop models. |
| `Icod.CommandFramework.RegularExpressions` | Fully managed GNU Basic, GNU Extended, and GNU Emacs regular-expression profiles with POSIX/GNU leftmost-longest matching, captures, locale-aware character classes, cancellation/resource limits, and exact byte-coordinate matching. |
| `Icod.CommandFramework.Temporary` | Cryptographically strong temporary-name generation, exclusive file/directory creation, template handling, collision reporting, and disposable temporary workspaces. |
| `Icod.CommandFramework.Host` | Host and processor-resource observations with explicit availability, provenance, and semantic-fidelity information. |
| `Icod.CommandFramework.Time` | Monotonic time and periodic scheduling primitives suitable for timeout, polling, and process-control code. |

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

Canonical-path resolution is intentionally maintained in the separate `Icod.Path` package and is referenced by `Icod.CommandFramework`.

### Process execution and control

The process layer is designed for programs that must supervise or control other programs rather than merely call `Process.Start()`.

It includes executable resolution, explicit environment construction, exact argument-vector launch, asynchronous standard-stream handling, process identity and PID-reuse protection, cancellation and monotonic timeout behavior, process-tree/process-group handling, waiting and liveness checks, signal translation and delivery where supported, and portable priority/nice abstractions.

Platform differences remain visible. Unsupported signal, group, session, or priority semantics return controlled results instead of being silently reinterpreted.

See [`src/Processes/README.md`](src/Processes/README.md).

### Terminal presentation and control

Command behavior often changes when output is redirected, when a terminal has a known width, or when color/control sequences are available.

The terminal layer provides injectable observation and control contracts for stream attachment, terminal size, `TERM`/related environment data, color capability, filename presentation, and platform-specific terminal modes and control operations. Unix and Windows implementations share common contracts without claiming that the underlying terminal APIs are identical.

See [`src/Terminal/README.md`](src/Terminal/README.md).

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

### Host and platform observations

Portable tools should not convert “unsupported,” “temporarily unavailable,” and “not applicable” into the same magic value.

Host-resource APIs carry availability and provenance information, and observation fidelity distinguishes exact, equivalent, approximated, synthesized, and unavailable data. The platform layer likewise exposes capability and operation-result contracts for services such as system identity and security contexts.

This makes it possible for a command to decide whether approximation is acceptable instead of having the framework make that policy decision invisibly.

See [`src/Host/README.md`](src/Host/README.md).

## What this library is not

`Icod.CommandFramework` is intentionally a low-level command infrastructure library.

It is **not**:

- a command-dispatch or dependency-injection framework;
- a shell, shell parser, or subprocess scripting language;
- a replacement for command-specific option validation or help generation;
- an implementation of GNU Coreutils;
- a license to delegate work to installed native utilities;
- a promise that every Unix concept can be emulated perfectly on every operating system.

The library supplies mechanisms that can be shared safely. Programs built on it remain responsible for their own externally visible contract.

## Who is it for?

`Icod.CommandFramework` is most useful when you are building:

- cross-platform command-line utilities;
- Unix/POSIX-compatible tools in managed code;
- file, text, archive, process, or terminal utilities;
- applications that consume large streams or byte-delimited data;
- testable CLI engines that should not read directly from process-global console/environment state;
- tools that need explicit platform capability reporting rather than optimistic emulation;
- software that needs GNU/POSIX regular-expression behavior rather than .NET regex behavior.

For a small application that only needs a few switches and writes ordinary text to `Console.Out`, a smaller CLI package may be a better fit. This framework earns its keep when platform behavior, byte fidelity, process control, streaming, or Unix compatibility become part of the program's contract.

## Platform and target framework

The package targets:

- **.NET 10**
- **C# 13**

The repository CI builds and tests on:

- Windows
- Ubuntu Linux
- macOS

Some capabilities are naturally platform-specific. For example, SELinux support requires Linux and a usable `libselinux`, while signal, process-group, terminal-control, filesystem-metadata, and identity behavior varies by host. Platform-specific features are exposed through capability/provider contracts so consumers can handle those differences explicitly.

## Package dependency

`Icod.CommandFramework` depends on:

- `Icod.Path` — canonical and platform-aware path infrastructure.

Command-suite-specific packages are deliberately **not** dependencies of the framework.

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

1. **Do not hide observable behavior.** Byte offsets, record termination, platform support, approximation, and process failures are often part of a command's public contract.
2. **Do not turn every expected failure into an exception.** Ordinary races, unsupported operations, parse failures, and process results should be representable as controlled data.
3. **Do not make commands depend on one another.** Reusable infrastructure belongs below command suites; suite-specific policy belongs above it.
4. **Do not shell out to solve portability.** Native APIs may be used when necessary, but the framework is not implemented by invoking the host's copy of the command being reimplemented.
5. **Keep environmental dependencies injectable.** Doing so makes both portability and testing substantially easier.
6. **Prefer bounded algorithms for unbounded input.** Pipes, files, records, and child output can be arbitrarily large.
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
    Host/
    IO/
    Platform/
    Processes/
    Records/
    RegularExpressions/
    Temporary/
    Terminal/
    Text/
    Time/
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

That history matters because these APIs were not designed only from hypothetical abstractions: they were exercised against concrete requirements involving GNU-style option parsing, byte-oriented pipelines, filesystem edge cases, POSIX process behavior, terminal handling, regular-expression compatibility, and cross-platform tests.

The standalone package exists so that infrastructure can now be used independently.

## License

Licensed under **LGPL-3.0-or-later**. See [`LICENSE`](LICENSE).
