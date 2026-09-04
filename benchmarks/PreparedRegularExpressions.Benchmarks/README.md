# Immutable prepared-input regular-expression benchmark

This candidate-only BenchmarkDotNet project measures the R2.3 reuse ceiling without changing the public `ICompiledRegularExpression` contract.

It compares two ways of enumerating every `TARGET` match in the same 64 KiB authoritative byte record:

- `PublicMatchLoop` uses the existing public `Match` API and therefore prepares the record for every call.
- `PreparedMatchLoop` prepares one immutable, privately owned input once and reuses it for every match.

The project is intentionally separate from `RegularExpressions.Benchmarks`. The pinned `2.1.0` comparison collector overlays that original benchmark project into the baseline worktree; placing candidate-only internal APIs there would make the immutable historical baseline impossible to build.

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
2. restores and builds the candidate-only benchmark in Release;
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

The prepared input owns a defensive copy of authoritative bytes. Matching creates fresh per-call context/state, so one prepared input and one compiled expression can be used concurrently. Returned byte values are copied from the private prepared source so a result cannot mutate later matches.

This project establishes the potential benefit before any public prepared-input API is considered. R2.3 should prefer internal consumer integration unless a real cross-repository consumer demonstrates that a public immutable type is necessary.
