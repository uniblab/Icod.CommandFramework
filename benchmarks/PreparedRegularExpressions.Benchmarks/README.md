# Immutable prepared-input regular-expression benchmark

This BenchmarkDotNet project measures the R2.3 reuse benefit through the package-ready **public immutable byte-input API** without changing the `ICompiledRegularExpression` interface.

It compares two ways of enumerating every `TARGET` match in the same 64 KiB authoritative byte record:

- `PublicMatchLoop` uses the ordinary public `Match` API and therefore prepares the record for every call.
- `PreparedMatchLoop` creates one public `RegularExpressionPreparedByteInput` and reuses it through the public prepared-input `Match` extension.

The project remains intentionally separate from `RegularExpressions.Benchmarks`. The pinned `2.1.0` comparison collector overlays that original benchmark project into the baseline worktree, while the prepared-input project exercises only the current candidate package surface.

The benchmark project no longer requires `InternalsVisibleTo` access. A successful physical result therefore represents performance available to a real cross-assembly consumer such as Icod.Grep.

## Deterministic smoke

From the repository root:

```powershell
dotnet restore benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj
dotnet run --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj -c Staging -- --smoke
```

## Authoritative physical collection

Use the dedicated collector from a clean `regex-performance-2.2.0` worktree:

```powershell
powershell .\benchmarks\Collect-PreparedRegexComparison.ps1 `
    -Passes 2 `
    -CooldownSeconds 30
```

The collector:

1. records the exact candidate commit;
2. restores and builds the prepared-input benchmark in Release;
3. runs two independent in-process BenchmarkDotNet passes by default;
4. validates each pass independently;
5. waits 30 seconds between passes by default;
6. records the sibling `Icod.Grep/hardware_inventory.txt` SHA-256 when present;
7. records the .NET SDK version; and
8. writes `comparison.json` beside the per-pass artifacts beneath:

```text
artifacts/performance/regex-prepared-input-candidate-1/
```

A direct exploratory run remains available:

```powershell
dotnet run `
    --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj `
    -c Release `
    -- `
    --inProcess `
    --artifacts artifacts/performance/regex-prepared-input-exploratory
```

`RegularExpressionPreparedByteInput` owns a defensive copy of authoritative bytes. Matching creates fresh per-call context/state, so one prepared input and one compiled expression can be reused concurrently. Returned byte values are isolated from the private prepared source.

R2.3 Candidate 1 established the internal reuse ceiling. Candidate 2 uses this same benchmark to verify that the public immutable consumer surface retains that benefit without exposing matcher internals, mutable cursors, pooling, or shared match state.
