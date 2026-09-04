# Icod.CommandFramework R2 regular-expression benchmarks

This directory contains the direct managed regular-expression performance harness for the `2.2.0` R2 roadmap.

The benchmark project is intentionally outside `Icod.CommandFramework.sln`, is not packable, and uses only public regular-expression APIs. Production packages therefore do not acquire BenchmarkDotNet or benchmark sources.

## Benchmark groups

- `RegexLiteralSearchBenchmarks` measures unanchored BRE/ERE literal searching at different input lengths and hit positions.
- `RegexDecodeBenchmarks` requires a match at byte offset zero so full-input decode/setup cost can be observed with only one search start.
- `RegexStructuralBenchmarks` covers alternation, repetition, bracket classes, assertions, captures, backreferences, anchors, and empty matches.
- `RegexRepeatedSearchBenchmarks` repeatedly searches one 64 KiB record, modeling consumers that enumerate multiple matches from the same input.
- `RegexCompileBenchmarks` isolates pattern compilation cost.

All matching workloads use the public `ICompiledRegularExpression` byte-preserving surface. The C/POSIX character-class provider is used unless the scenario specifically targets UTF-8 decoding.

## Current R2 status

- **R2.0** established the pinned `2.1.0` physical baseline and quantified the benchmark noise floor.
- **R2.1** is complete. Conservative complete-literal and required-prefix acceleration substantially reduced repeated unanchored start attempts.
- **R2.2** is complete. Accepted Candidate 3 uses a single-state deterministic `SequenceRegexNode` path while isolating the legacy collection iterator for branching/capturing/fallback cases and finite `MaximumMatchStates`.
- The next gate is a **whole-suite physical remeasurement** with both accepted tranches in place. That measurement decides whether R2.3 decode/prepared-input work remains the highest-value next target.

The retained quantitative reports live at the repository root. In particular, see `Icod.CommandFramework-2.2.0-R2.2-Candidate-3.md` for the accepted deterministic-sequence result.

## Deterministic smoke

From the repository root:

```powershell
dotnet restore benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj
dotnet run --project benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Staging -- --smoke
```

The smoke validates representative BRE/ERE matches, misses, structural expressions, valid UTF-8, malformed UTF-8 Preserve/Replace behavior, and malformed UTF-8 Throw behavior. It is a correctness/portability check rather than a performance gate.

## BenchmarkDotNet

A normal local run can be filtered, for example:

```powershell
dotnet run --project benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Release -- --filter "*RegexLiteralSearchBenchmarks*"
```

For focused optimization work, prefer the smallest benchmark class/scenario that exercises the code being changed. Whole-suite runs are appropriate for baseline and tranche closure, not for every edit.

## Physical reference comparison

`Collect-RegexReferenceComparison.ps1` compares the pinned `2.1.0` commit with the current candidate while using the current benchmark harness for both variants. Both variants are created as fresh sibling detached worktrees so build-cache and developer-worktree differences do not become part of the comparison.

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

For the post-R2.2 gate, use `-Filter '*'` so literal search, decode, structural, repeated-search, and compile workloads are remeasured together. Do not choose the R2.3 implementation solely from the original R2.0 ordering; R2.1 and R2.2 changed the cost landscape substantially.

Timing measurements on hosted CI runners are observational only. Managed allocation measurements and controlled repeated measurements on the physical reference host are the primary R2 optimization evidence.
