# Icod.CommandFramework 2.2.0 — R2.3 Candidate 1

**Tranche:** R2.3 — immutable prepared-input investigation  
**Post-R2.2 decision report:** `Icod.CommandFramework-2.2.0-Post-R2.2-Whole-Suite-Report.md`  
**Status:** implementation and semantic/CI validation in progress; physical reuse-ceiling measurement pending

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

## Candidate-only benchmark

`benchmarks/PreparedRegularExpressions.Benchmarks` compares two methods over the same 64 KiB record:

- `PublicMatchLoop` repeatedly calls the unchanged public API;
- `PreparedMatchLoop` prepares once outside the measured operation and repeatedly calls the internal prepared matcher.

The project is separate from the pinned-baseline benchmark harness because the `2.1.0` worktree cannot compile candidate-only internal types. The comparison is therefore within one candidate build on one process/host and measures the reuse ceiling directly.

Deterministic smoke verifies both paths enumerate the same nonzero number of matches. PR CI builds and runs that smoke on Windows, Linux, and macOS and exercises a separately validated BenchmarkDotNet Dry job on Windows. The original and prepared BenchmarkDotNet runs use independent artifact directories so stale or unrelated results cannot satisfy the wrong validation gate.

## Explicit non-goals

Candidate 1 does not:

- add a public prepared-input API;
- cache arbitrary caller inputs inside compiled expressions;
- introduce pooling;
- retain caller-owned mutable byte storage;
- share match contexts or resource counters between calls;
- weaken malformed-input or coordinate contracts; or
- alter the public one-shot matching implementation already accepted in R2.1/R2.2.

## Acceptance gate

After canonical CI is green, run the dedicated collector from a clean physical-reference worktree:

```powershell
powershell .\benchmarks\Collect-PreparedRegexComparison.ps1 `
    -Passes 2 `
    -CooldownSeconds 30
```

The collector validates each BenchmarkDotNet pass independently and records the candidate commit, pass sequence, hardware inventory SHA-256 when available, .NET SDK version, and collection time in `comparison.json` beneath:

```text
artifacts/performance/regex-prepared-input-candidate-1/
```

Candidate 1 should be retained as the R2.3 foundation only if:

1. prepared repeated-search allocation is materially below the current public ~73.3 MiB result;
2. prepared and public match counts are identical;
3. prepared timing improves materially enough to justify integration complexity;
4. public whole-suite behavior remains unchanged;
5. all immutability, concurrency, coordinate, malformed-input, cancellation, capture, and resource-limit tests remain green; and
6. no cross-platform CI regression is introduced.

A favorable result authorizes the next design decision: expose immutable preparation only as far as a real consumer needs. A public API remains contingent on Icod.Grep or another cross-assembly consumer proving that internal integration cannot deliver the measured benefit.
