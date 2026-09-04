# Icod.CommandFramework R2 regular-expression benchmarks

This directory contains the direct managed regular-expression performance harness for the `2.2.0` R2 roadmap.

The benchmark projects are intentionally outside `Icod.CommandFramework.sln` and are not packable. Production packages therefore do not acquire BenchmarkDotNet or benchmark sources.

## Pinned-baseline benchmark groups

`RegularExpressions.Benchmarks` uses only public regular-expression APIs and remains compatible with the immutable `2.1.0` baseline worktree.

- `RegexLiteralSearchBenchmarks` measures unanchored BRE/ERE literal searching at different input lengths and hit positions.
- `RegexDecodeBenchmarks` requires a match at byte offset zero so full-input decode/setup cost can be observed with only one search start.
- `RegexStructuralBenchmarks` covers alternation, repetition, bracket classes, assertions, captures, backreferences, anchors, and empty matches.
- `RegexRepeatedSearchBenchmarks` repeatedly searches one 64 KiB record, modeling consumers that enumerate multiple matches from the same input.
- `RegexCompileBenchmarks` isolates pattern compilation cost.

All matching workloads use the public `ICompiledRegularExpression` byte-preserving surface. The C/POSIX character-class provider is used unless the scenario specifically targets UTF-8 decoding.

## Candidate-only prepared-input benchmark

`PreparedRegularExpressions.Benchmarks` is the R2.3 candidate-only reuse-ceiling harness. It compares repeated public `Match` calls with repeated matching over one immutable prepared 64 KiB record.

The project is deliberately separate from the pinned-baseline harness. The comparison collector overlays `RegularExpressions.Benchmarks` into the historical `2.1.0` worktree; candidate-only internal APIs cannot be introduced into that build without contaminating or breaking the baseline.

See [`PreparedRegularExpressions.Benchmarks/README.md`](PreparedRegularExpressions.Benchmarks/README.md) for the exact smoke and physical commands.

## Current R2 status

- **R2.0** established the pinned `2.1.0` physical baseline and quantified the benchmark noise floor.
- **R2.1** is complete. Conservative complete-literal and required-prefix acceleration substantially reduced repeated unanchored start attempts.
- **R2.2** is complete. Accepted Candidate 3 uses a single-state deterministic `SequenceRegexNode` path while isolating the legacy collection iterator for branching/capturing/fallback cases and finite `MaximumMatchStates`.
- The post-R2.2 whole-suite comparison confirmed that decode/materialization and repeated preparation are now the dominant residual costs.
- **R2.3 Candidate 1** is in progress. It establishes an internal immutable prepared-input seam and directly measures the reuse ceiling before any public API is considered.

The retained quantitative reports live at the repository root. See:

- `Icod.CommandFramework-2.2.0-R2.2-Candidate-3.md`;
- `Icod.CommandFramework-2.2.0-Post-R2.2-Whole-Suite-Report.md`; and
- `Icod.CommandFramework-2.2.0-R2.3-Candidate-1.md`.

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

The R2.3 prepared-input reuse-ceiling run is:

```powershell
dotnet run --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj -c Release -- --inProcess
```

For focused optimization work, prefer the smallest benchmark class/scenario that exercises the code being changed. Whole-suite runs are appropriate for baseline and tranche closure, not for every edit.

## Physical reference comparison

`Collect-RegexReferenceComparison.ps1` compares the pinned `2.1.0` commit with the current candidate while using the current public benchmark harness for both variants. Both variants are created as fresh sibling detached worktrees so build-cache and developer-worktree differences do not become part of the comparison.

On the physical Windows reference laptop, the normal authoritative form is:

```powershell
powershell .\benchmarks\Collect-RegexReferenceComparison.ps1 `
    -Filter '*' `
    -Passes 2 `
    -CooldownSeconds 30
```

The default collection also performs two alternating passes in ABBA order with a 30-second cooldown, so the shorter equivalent remains valid:

```powershell
powershell .\benchmarks\Collect-RegexReferenceComparison.ps1
```

Results are written beneath:

```text
artifacts/performance/regex-reference-comparison/
```

Use `-Filter` to narrow the BenchmarkDotNet group, `-Passes` to change the number of alternating passes, and `-CooldownSeconds` to change the inter-run interval.

Timing measurements on hosted CI runners are observational only. Managed allocation measurements and controlled repeated measurements on the physical reference host are the primary R2 optimization evidence.
