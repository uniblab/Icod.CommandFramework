# Icod.CommandFramework 2.2.0 — R2.3 Candidate 1

**Tranche:** R2.3 — immutable prepared-input investigation  
**Post-R2.2 decision report:** `Icod.CommandFramework-2.2.0-Post-R2.2-Whole-Suite-Report.md`  
**Status:** accepted as the R2.3 immutable prepared-input foundation

## Motivation

The post-R2.2 whole-suite comparison established that R2.1 and R2.2 removed the dominant repeated-start and deterministic sequence-collection costs, while large anchored input preparation remained almost unchanged.

The strongest residual signal was repeated matching over one 64 KiB record. R2.1/R2.2 reduced that workload from roughly 101.3 MiB to 73.3 MiB allocated per operation, but every public `Match` call still decodes and materializes the same authoritative source again.

Candidate 1 measures and validates the smallest safe reuse boundary before considering any public API.

## Immutable prepared input

`PreparedRegexInput` is internal and immutable by construction:

- string preparation retains the already-immutable source string;
- byte preparation takes a defensive copy before decoding;
- decoded matching units, opaque-unit flags, and source-boundary arrays remain private behind the existing read-only `RegexInput` operations;
- no mutable match context or match state is stored on the prepared input; and
- byte match/capture values produced through the prepared path are copied from the private prepared source so returned results cannot mutate later matches.

One prepared input may therefore be matched concurrently. Each call creates its own `RegexMatchContext`, state count, initial states, captures, and result objects.

## Internal matching seam

`IPreparedCompiledRegularExpression` adds internal string and authoritative-byte `MatchPrepared` operations. It is deliberately not public in Candidate 1.

All GNU providers return an outer `PreparedCompiledRegularExpression` that:

1. delegates every existing public member to the already-accepted compiled expression stack;
2. retains the immutable AST/options/character-provider references needed for prepared matching;
3. reproduces the accepted complete-literal and required-prefix eligibility rules for prepared input;
4. invokes the existing AST matcher over the already-decoded representation; and
5. preserves the general state-accounting path whenever `MaximumMatchStates` is finite.

The existing public `ICompiledRegularExpression` contract is unchanged. Public callers therefore receive exactly the R2.1/R2.2 behavior already physically accepted.

## Parallel-execution properties

Candidate 1 is designed so parallel execution does not require locks:

- compiled ASTs and option records are immutable after compilation;
- prepared source and source-coordinate mappings are immutable after preparation;
- prefix arrays are private and never exposed;
- every search allocates an independent match context and match-state graph; and
- result objects do not expose the prepared byte buffer.

The semantic suite includes concurrent matching over one compiled expression and one prepared byte input from 128 tasks with different start offsets.

## Semantic coverage

Focused tests compare public and prepared byte matching across:

- complete literals;
- alternation;
- optional grouped structure;
- word-boundary assertions;
- captures/backreferences; and
- bounded repetition.

Additional tests cover:

- defensive ownership against mutation of the caller's original byte array;
- isolation from mutation of a prior returned byte result;
- concurrent reuse;
- exact UTF-8 source boundaries and split-boundary rejection;
- UTF-16 string coordinates;
- malformed UTF-8 `PreserveBytes`, `Replace`, and `Throw` behavior;
- finite `MaximumMatchStates` diagnostics;
- cancellation during preparation and prepared matching;
- rejection of text/byte prepared-input kind mismatches; and
- prepared matching through GNU Basic, GNU Extended, and GNU Emacs providers.

Canonical PR workflow **33884416156** is green at Candidate 1 validation head `b6c52f85feef7669504b3fc3f77e98a63d89821e`. It includes 502 tests on Windows, Linux, and macOS, both benchmark-project smokes, independent BenchmarkDotNet artifact validation, and both collector smokes.

## Candidate-only benchmark

`benchmarks/PreparedRegularExpressions.Benchmarks` compares two methods over the same 64 KiB record:

- `PublicMatchLoop` repeatedly calls the unchanged public API;
- `PreparedMatchLoop` prepares once outside the measured operation and repeatedly calls the internal prepared matcher.

The project is separate from the pinned-baseline benchmark harness because the `2.1.0` worktree cannot compile candidate-only internal types. The comparison is therefore within one candidate build on one process/host and measures the reuse ceiling directly.

Deterministic smoke verifies both paths enumerate the same nonzero number of matches. The original and prepared BenchmarkDotNet runs use independent artifact directories so stale or unrelated results cannot satisfy the wrong validation gate.

## Physical reuse-ceiling measurement

The authoritative physical collection was made from Candidate 1 commit `b6c52f85feef7669504b3fc3f77e98a63d89821e` using:

```powershell
powershell .\benchmarks\Collect-PreparedRegexComparison.ps1 `
    -Passes 2 `
    -CooldownSeconds 30
```

Collection metadata:

- BenchmarkDotNet `0.15.8`;
- InProcess toolchain;
- .NET runtime `10.0.11`;
- .NET SDK `10.0.400`;
- Windows `10.0.26200.9168`;
- AMD Ryzen 7 5700U, 16 logical / 8 physical cores;
- concurrent workstation GC; and
- hardware inventory SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`.

Both passes completed two executed benchmarks and produced validated result artifacts.

| Pass | Public time | Prepared time | Time reduction | Public allocation | Prepared allocation | Allocation reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 49.739 ms | 0.243 ms | 99.51% | 74,973.47 KiB | 9.06 KiB | 99.9879% |
| 2 | 52.322 ms | 0.260 ms | 99.50% | 74,973.43 KiB | 9.06 KiB | 99.9879% |
| Two-pass mean | 51.031 ms | 0.252 ms | **99.51%** | 73.216 MiB | 9.06 KiB | **99.9879%** |

The two-pass mean corresponds to approximately **203× higher throughput** and **8,275× less managed allocation** for repeated matching after one immutable preparation.

The result is substantially larger than the original R2.3 acceptance threshold and is reproduced independently in both physical passes. Public-loop allocation is byte-for-byte stable to within 0.04 KiB across passes; prepared-loop allocation is exactly 9.06 KiB in both passes.

## Acceptance decision

**Candidate 1 is accepted as the R2.3 foundation.**

The measurement proves that repeated decode/materialization, not residual state traversal, is the dominant cost for the repeated-search workload. Immutable preparation removes essentially all of that cost while preserving independent per-call match state and the existing semantic contracts.

This does **not** by itself authorize a broad public API. The next R2.3 step is to inspect the actual Icod.Grep repeated-match integration and choose the smallest immutable consumable seam that can capture the measured benefit. The preferred order is:

1. determine whether an existing higher-level consumer operation can prepare once internally;
2. if cross-assembly reuse requires a package surface, design the smallest public immutable prepared-input contract;
3. avoid exposing matcher internals, mutable cursors, pooling, or shared state; and
4. validate the chosen seam through Icod.Grep before closing R2.3.

## Explicit non-goals retained

Candidate 1 does not:

- add a public prepared-input API;
- cache arbitrary caller inputs inside compiled expressions;
- introduce pooling;
- retain caller-owned mutable byte storage;
- share match contexts or resource counters between calls;
- weaken malformed-input or coordinate contracts; or
- alter the public one-shot matching implementation already accepted in R2.1/R2.2.
