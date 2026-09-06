# Immutable prepared-input regular-expression benchmarks

This BenchmarkDotNet project measures performance available through the package-ready **public immutable byte-input API** without changing the `ICompiledRegularExpression` interface.

It contains two focused benchmark groups.

## Repeated matching over one prepared record

`PreparedRegexInputBenchmarks` compares two ways of enumerating every `TARGET` match in the same 64 KiB authoritative byte record:

- `PublicMatchLoop` uses the ordinary public `Match` API and therefore prepares the record for every call.
- `PreparedMatchLoop` creates one public `RegularExpressionPreparedByteInput` and reuses it through the public prepared-input `Match` extension.

This benchmark was the R2.3 acceptance harness for the public prepared-input surface shipped in 2.2.0.

## Large byte-mode preparation

`PreparedByteInputConstructionBenchmarks` isolates construction of one 1 MiB immutable prepared input in `TextDecodingMode.Bytes`:

```csharp
RegularExpressionPreparedByteInput.Prepare(
    input,
    inputOptions
)
```

This benchmark was added for the 2.2.1 memory follow-up after Icod.Grep T6.8 large-record stress testing exposed excessive transient allocation in the byte-mode decode path.

The benchmark intentionally measures the public preparation API rather than an internal helper. It therefore includes the defensive authoritative-byte snapshot plus construction of the immutable decoded representation exactly as a real cross-assembly consumer observes it.

The accepted physical comparison against stable 2.2.0 measured allocation falling from approximately **19.01 MiB to 10.00 MiB per 1 MiB input**, a **47.36% reduction**. Both candidate timing passes were also materially faster than the fastest baseline timing pass. The retained result is documented in `Icod.CommandFramework-2.2.1-Byte-Input-Memory-Closure.md`.

## Prepared-input ownership and concurrency contract

`RegularExpressionPreparedByteInput` owns a defensive copy of authoritative bytes. Caller mutation after preparation cannot affect later matches.

Matching creates fresh per-call context/state, so one prepared input and one compiled expression can be reused concurrently. Returned byte values do not expose the private prepared snapshot for mutation. Third-party matcher fallback receives an isolated copy rather than the prepared object's private source.

## Deterministic smoke

From the repository root:

```powershell
dotnet restore benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj
```

```powershell
dotnet run --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj -c Staging -- --smoke
```

The smoke verifies that ordinary and prepared matching enumerate the same nonzero match count. It is a correctness/portability gate, not a timing gate.

## R2.3 prepared-input reuse collection

The retained R2.3 collector measures the repeated-match prepared-input benefit from the current candidate:

```powershell
powershell .\benchmarks\Collect-PreparedRegexComparison.ps1 -Passes 2 -CooldownSeconds 30
```

It records the exact candidate commit, performs independent BenchmarkDotNet passes, validates each artifact set, records the physical-reference hardware hash when available, and writes `comparison.json` beneath `artifacts/performance/regex-prepared-input-candidate-2/`.

## 2.2.1 byte-input construction comparison

For the 2.2.1 byte-mode memory gate, use the dedicated baseline-versus-candidate collector from a clean worktree:

```powershell
powershell .\benchmarks\Collect-PreparedByteInputConstructionComparison.ps1 -BaselineCommit '3c7b8189f664be67b74fb71809580d3133dc135f' -BaselineLabel 'CommandFramework-2.2.0' -OutputDirectory 'artifacts/performance/byte-input-construction' -Passes 2 -CooldownSeconds 30
```

The collector is compatible with Windows PowerShell 5.1. It:

1. records the exact baseline and candidate commits;
2. creates a short detached baseline worktree under `%TEMP%` to avoid Windows path-length cleanup failures;
3. overlays only `PreparedByteInputConstructionBenchmarks.cs` into the stable 2.2.0 worktree;
4. restores and builds baseline and candidate in Release;
5. runs two alternating ABBA passes by default;
6. validates every BenchmarkDotNet artifact directory;
7. waits 30 seconds between runs by default;
8. records the sibling `Icod.Grep/hardware_inventory.txt` SHA-256 when present; and
9. writes `comparison.json` beside the four run directories.

Results are written beneath:

```text
artifacts/performance/byte-input-construction/
```

A direct exploratory construction run remains available:

```powershell
dotnet run --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj -c Release -- --inProcess --filter "*PreparedByteInputConstructionBenchmarks*"
```

Physical timing should be interpreted together with run order and variance. Managed allocation is the primary 2.2.1 acceptance signal because it was extremely stable across both baseline and candidate passes.
