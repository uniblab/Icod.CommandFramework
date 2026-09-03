# Icod.CommandFramework R2 regular-expression benchmarks

This directory contains the direct managed regular-expression performance harness for the `2.2.0` R2 roadmap.

The benchmark project is intentionally outside `Icod.CommandFramework.sln`, is not packable, and uses only public regular-expression APIs. Production packages therefore do not acquire BenchmarkDotNet or benchmark sources.

## R2.0 benchmark groups

- `RegexLiteralSearchBenchmarks` measures unanchored BRE/ERE literal searching at different input lengths and hit positions.
- `RegexDecodeBenchmarks` requires a match at byte offset zero so full-input decode/setup cost can be observed with only one search start.
- `RegexStructuralBenchmarks` covers alternation, repetition, bracket classes, assertions, captures, backreferences, anchors, and empty matches.
- `RegexRepeatedSearchBenchmarks` repeatedly searches one 64 KiB record, modeling consumers that enumerate multiple matches from the same input.
- `RegexCompileBenchmarks` isolates pattern compilation cost.

All matching workloads use the public `ICompiledRegularExpression` byte-preserving surface. The C/POSIX character-class provider is used unless the scenario specifically targets UTF-8 decoding.

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

## R2.0 2.1.0 baseline

`Collect-RegexReferenceComparison.ps1` compares the pinned `2.1.0` commit with the current candidate while using the current benchmark harness for both variants. Both variants are created as fresh sibling detached worktrees so build-cache and developer-worktree differences do not become part of the comparison.

On the physical Windows reference laptop:

```powershell
./benchmarks/Collect-RegexReferenceComparison.ps1
```

The default collection performs two alternating passes in ABBA order with a 30-second cooldown and writes results beneath:

```text
artifacts/performance/regex-reference-comparison/
```

Use `-Filter` to narrow the BenchmarkDotNet group, `-Passes` to change the number of alternating passes, and `-CooldownSeconds` to change the inter-run interval.

Timing measurements on hosted CI runners are observational only. Managed allocation measurements and controlled repeated measurements on the physical reference host are the primary R2 optimization evidence.
