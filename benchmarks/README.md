# Icod.CommandFramework regular-expression benchmarks

This directory contains the direct managed regular-expression performance harness used for the `2.2.x` R2 performance work and the `2.2.1` byte-input memory follow-up.

The benchmark projects are intentionally outside `Icod.CommandFramework.sln` and are not packable. Production packages therefore do not acquire BenchmarkDotNet or benchmark sources.

## Pinned-baseline benchmark groups

`RegularExpressions.Benchmarks` uses only public regular-expression APIs and remains compatible with the immutable `2.1.0` baseline worktree.

- `RegexLiteralSearchBenchmarks` measures unanchored BRE/ERE literal searching at different input lengths and hit positions.
- `RegexDecodeBenchmarks` requires a match at byte offset zero so full-input decode/setup cost can be observed with only one search start.
- `RegexStructuralBenchmarks` covers alternation, repetition, bracket classes, assertions, captures, backreferences, anchors, and empty matches.
- `RegexRepeatedSearchBenchmarks` repeatedly searches one 64 KiB record, modeling consumers that enumerate multiple matches from the same input.
- `RegexCompileBenchmarks` isolates pattern compilation cost.

All matching workloads use the public `ICompiledRegularExpression` byte-preserving surface. The C/POSIX character-class provider is used unless the scenario specifically targets UTF-8 decoding.

## Prepared-input benchmark groups

`PreparedRegularExpressions.Benchmarks` exercises the public immutable prepared-byte-input API introduced during R2.3.

- `PreparedRegexInputBenchmarks` compares repeated ordinary public `Match` calls with repeated matching over one immutable prepared 64 KiB record.
- `PreparedByteInputConstructionBenchmarks` isolates construction of one 1 MiB `TextDecodingMode.Bytes` prepared input through the public `RegularExpressionPreparedByteInput.Prepare(...)` API.

The prepared-input benchmark project remains separate from `RegularExpressions.Benchmarks` because the pinned historical baselines predate the public prepared-input surface. Dedicated collectors overlay only the benchmark source needed by a historical worktree so baseline production code remains unchanged.

See [`PreparedRegularExpressions.Benchmarks/README.md`](PreparedRegularExpressions.Benchmarks/README.md) for exact commands and interpretation.

## R2 and 2.2.1 status

- **R2.0** established the pinned `2.1.0` physical baseline and quantified the benchmark noise floor.
- **R2.1** completed conservative complete-literal and required-prefix acceleration.
- **R2.2** completed deterministic sequence specialization while preserving the general fallback path.
- The post-R2.2 whole-suite comparison identified decode/materialization and repeated preparation as the dominant residual costs.
- **R2.3** completed the immutable prepared-input design. Candidate 1 established the internal reuse ceiling; Candidate 2 exposed the package-ready public immutable prepared-byte-input surface; R2.5 validated that surface through the Icod.Grep consumer.
- **2.2.1** follows a large-record stress result from Icod.Grep T6.8. The accepted byte-mode construction candidate avoids list-plus-copy amplification by directly populating final decoded arrays when every source byte is exactly one matching unit.

The retained quantitative reports live at the repository root. Relevant reports include:

- `Icod.CommandFramework-2.2.0-R2.2-Candidate-3.md`;
- `Icod.CommandFramework-2.2.0-Post-R2.2-Whole-Suite-Report.md`;
- `Icod.CommandFramework-2.2.0-R2.3-Candidate-1.md`;
- `Icod.CommandFramework-2.2.0-R2.3-Candidate-2.md`;
- `Icod.CommandFramework-2.2.0-R2.4-Closure.md`;
- `Icod.CommandFramework-2.2.0-R2.5-Grep-Consumer-Validation.md`; and
- `Icod.CommandFramework-2.2.1-Byte-Input-Memory-Closure.md`.

## Deterministic smoke

From the repository root:

```powershell
dotnet restore benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj
dotnet run --project benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Staging -- --smoke

dotnet restore benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj
dotnet run --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj -c Staging -- --smoke
```

The pinned-baseline smoke validates representative BRE/ERE matches, misses, structural expressions, valid UTF-8, malformed UTF-8 Preserve/Replace behavior, and malformed UTF-8 Throw behavior. The prepared-input smoke verifies that public and immutable-prepared loops enumerate the same nonzero match count. Both are correctness/portability checks rather than performance gates.

## BenchmarkDotNet

A normal pinned-baseline-compatible run can be filtered, for example:

```powershell
dotnet run --project benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Release -- --filter "*RegexLiteralSearchBenchmarks*"
```

The R2.3 prepared-input reuse collection is:

```powershell
powershell .\benchmarks\Collect-PreparedRegexComparison.ps1 -Passes 2 -CooldownSeconds 30
```

The 2.2.1 byte-input construction comparison is:

```powershell
powershell .\benchmarks\Collect-PreparedByteInputConstructionComparison.ps1 -BaselineCommit '3c7b8189f664be67b74fb71809580d3133dc135f' -BaselineLabel 'CommandFramework-2.2.0' -OutputDirectory 'artifacts/performance/byte-input-construction' -Passes 2 -CooldownSeconds 30
```

Both collectors are compatible with Windows PowerShell 5.1. The 2.2.1 collector creates its detached baseline worktree beneath a short `%TEMP%` path to avoid Windows path-length cleanup failures, runs baseline and candidate in alternating ABBA order, validates every BenchmarkDotNet artifact set, records the physical-reference hardware hash when available, and writes `comparison.json` beside the run artifacts.

The accepted 2.2.1 physical result for a 1 MiB prepared byte-mode input was approximately 19.01 MiB allocated under 2.2.0 versus 10.00 MiB under the candidate, a **47.36% reduction**. Timing was also materially improved in both candidate passes.

For focused optimization work, prefer the smallest benchmark class/scenario that exercises the code being changed. Whole-suite runs are appropriate for baseline and tranche closure, not for every edit.

## Physical reference comparison

`Collect-RegexReferenceComparison.ps1` compares the pinned `2.1.0` commit with the current candidate while using the current public benchmark harness for both variants. Both variants are created as fresh detached worktrees so build-cache and developer-worktree differences do not become part of the comparison.

On the physical Windows reference host, the default collection performs two alternating passes with a 30-second cooldown:

```powershell
powershell .\benchmarks\Collect-RegexReferenceComparison.ps1
```

Results are written beneath:

```text
artifacts/performance/regex-reference-comparison/
```

Use `-Filter` to narrow the BenchmarkDotNet group, `-Passes` to change the number of alternating passes, and `-CooldownSeconds` to change the inter-run interval.

Timing measurements on hosted CI runners are observational only. Managed allocation measurements and controlled repeated measurements on the physical reference host are the primary optimization evidence.
