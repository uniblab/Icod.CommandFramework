# Immutable prepared-input regular-expression benchmark

This candidate-only BenchmarkDotNet project measures the R2.3 reuse ceiling without changing the public `ICompiledRegularExpression` contract.

It compares two ways of enumerating every `TARGET` match in the same 64 KiB authoritative byte record:

- `PublicMatchLoop` uses the existing public `Match` API and therefore prepares the record for every call.
- `PreparedMatchLoop` prepares one immutable, privately owned input once and reuses it for every match.

The project is intentionally separate from `RegularExpressions.Benchmarks`. The pinned `2.1.0` comparison collector overlays that original benchmark project into the baseline worktree; placing candidate-only internal APIs there would make the immutable historical baseline impossible to build.

From the repository root:

```powershell
dotnet restore benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj
dotnet run --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj -c Staging -- --smoke
```

For the physical candidate comparison:

```powershell
dotnet run --project benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj -c Release -- --inProcess
```

The prepared input owns a defensive copy of authoritative bytes. Matching creates fresh per-call context/state, so one prepared input and one compiled expression can be used concurrently. Returned byte values are copied from the private prepared source so a result cannot mutate later matches.

This project establishes the potential benefit before any public prepared-input API is considered. R2.3 should prefer internal consumer integration unless a real cross-repository consumer demonstrates that a public immutable type is necessary.
