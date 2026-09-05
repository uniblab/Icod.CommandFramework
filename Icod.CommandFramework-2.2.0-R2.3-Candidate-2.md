# Icod.CommandFramework 2.2.0 — R2.3 Candidate 2

**Tranche:** R2.3 — immutable prepared-input consumer surface  
**Foundation:** R2.3 Candidate 1 accepted  
**Status:** accepted; R2.3 closed

## Purpose

R2.3 Candidate 1 physically proved that reusing one immutable prepared byte input across repeated matches reduces the 64 KiB repeated-search workload from roughly 73.2 MiB to 9.06 KiB allocated and from roughly 51.0 ms to 0.252 ms on the physical reference host.

Candidate 2 turns that internal proof into the smallest package-ready consumer surface needed by real cross-assembly users such as Icod.Grep.

## Consumer evidence

Icod.Grep PR #12 remains frozen on `Icod.CommandFramework 2.1.0` while CommandFramework develops the shared optimization.

Inspection of `Icod.Grep/src/Command.cs` shows that its managed BRE/ERE path has exactly the repeated-preparation shape isolated by Candidate 1:

- `RegularExpressionPattern.Find` calls `ICompiledRegularExpression.Match` with the authoritative record bytes and a new start offset;
- `PatternSet.Find` may invoke that operation repeatedly while enforcing `-w` / `-x` acceptance;
- `PatternSet.FindAll` repeatedly calls `Find` over the same record for `-o` and color-span enumeration.

Therefore the Candidate 1 reuse ceiling is directly applicable to the Grep consumer architecture.

## Public surface

Candidate 2 deliberately does **not** add members to `ICompiledRegularExpression`.

Instead it adds:

- `RegularExpressionPreparedByteInput`, a sealed immutable prepared authoritative-byte value;
- `RegularExpressionPreparedByteInput.Prepare(...)`, which snapshots caller-owned bytes exactly once and captures the decoding policy used to build the immutable decoded representation;
- `CompiledRegularExpressionPreparedInputExtensions.Match(...)`; and
- `CompiledRegularExpressionPreparedInputExtensions.MatchAsync(...)`.

The public surface is byte-only in R2.3 because Grep is the motivating cross-assembly consumer and uses authoritative byte records. The internal string-prepared path remains internal until a real consumer demonstrates a need for a public text-prepared API.

## Compatibility policy

The existing `ICompiledRegularExpression` interface is unchanged, preserving source and binary compatibility for existing implementations.

When the extension methods receive a CommandFramework compiled GNU expression, they use the internal prepared matcher and therefore avoid repeated decode/materialization.

When they receive a third-party `ICompiledRegularExpression` implementation that does not expose the internal prepared matcher, they fall back to that implementation's existing byte `Match` / `MatchAsync` methods. The fallback receives a fresh copy of the prepared source rather than the prepared object's private array. This deliberately trades fallback allocation for a strong immutability boundary: even an external implementation that recovers and mutates the `ReadOnlyMemory<byte>` backing array cannot alter the reusable prepared snapshot.

This preserves correctness and source/binary compatibility without requiring external implementations to understand CommandFramework internals or weakening the immutable ownership contract.

## Immutability and parallel execution

`RegularExpressionPreparedByteInput`:

- copies caller-owned authoritative bytes once during preparation;
- never exposes a mutable reference to that private snapshot;
- captures immutable `RegularExpressionInputOptions`;
- owns the immutable decoded/source-boundary representation used by native CommandFramework matching;
- stores no mutable match context, capture state, traversal state, or resource counter; and
- may therefore be reused concurrently by independent match calls without locks.

Returned byte match/capture values remain isolated from the prepared source, preserving the Candidate 1 mutation-safety contract.

The public API tests additionally include an adversarial external `ICompiledRegularExpression` implementation that attempts to mutate the array backing its fallback input. A subsequent native prepared match must still observe the original authoritative bytes.

## Benchmark gate

The candidate-only prepared-input benchmark uses only the **public** Candidate 2 API. The benchmark project no longer requires `InternalsVisibleTo` access.

The two methods are:

- `PublicMatchLoop`: ordinary public `Match` calls that prepare the same 64 KiB record repeatedly;
- `PreparedMatchLoop`: public `RegularExpressionPreparedByteInput` plus the public prepared-input `Match` extension.

This makes the physical measurement representative of the package surface available to a real external consumer rather than an internal friend assembly.

The authoritative Candidate 2 collector output is intentionally distinct from Candidate 1:

```text
artifacts/performance/regex-prepared-input-candidate-2/
```

## Canonical CI acceptance

Canonical PR workflow **33933148479** completed successfully at Candidate 2 head `4732442a6187de95817db610cbde8e028ca83b07`.

The green workflow validates the public prepared-input implementation and complete CommandFramework test/benchmark gates across the supported CI matrix. The prepared benchmark itself has no friend-assembly access, so its build and smoke validate the same public API shape intended for Icod.Grep.

## Authoritative physical result

The authoritative physical archive records:

- candidate commit `4732442a6187de95817db610cbde8e028ca83b07`;
- BenchmarkDotNet InProcess mode;
- two non-smoke passes;
- 30-second recorded cooldown;
- .NET SDK `10.0.400`;
- reference hardware inventory SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`.

Both passes completed both benchmark methods and produced validated BenchmarkDotNet CSV reports.

| Pass | Public time | Prepared time | Time reduction | Public allocation | Prepared allocation | Allocation reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 43.063 ms | 0.210 ms | 99.51% | 74,973.47 KiB | 9.06 KiB | 99.9879% |
| 2 | 39.102 ms | 0.214 ms | 99.45% | 74,973.44 KiB | 9.06 KiB | 99.9879% |
| Two-pass mean | **41.083 ms** | **0.212 ms** | **99.48%** | **73.216 MiB** | **9.06 KiB** | **99.9879%** |

The two-pass mean corresponds to approximately **194× higher throughput** and **8,275× less managed allocation** for repeated matching through the public immutable prepared-input API.

The allocation result reproduces Candidate 1 exactly: **9.06 KiB in both passes**. Prepared timing is also within and slightly better than the Candidate 1 physical result of roughly 0.252 ms. The public API layer therefore adds no material reuse penalty.

## Acceptance decision

**Candidate 2 is accepted, and R2.3 is closed.**

All acceptance gates are satisfied:

1. canonical Windows/Linux/macOS CI and the complete test/benchmark suite are green;
2. the public prepared-input benchmark smoke verifies the same nonzero match count as the ordinary public path;
3. the physical public-only two-pass run reproduces Candidate 1's allocation result exactly and retains its timing class;
4. ordinary `ICompiledRegularExpression` callers and implementations are unchanged;
5. third-party implementations retain a correct fallback without mutable access to the prepared snapshot; and
6. no mutable cursor, cache, pooling contract, decoded buffer, state machine, or matcher-internal type is public.

## R2.3 closure contract

The retained package contract is deliberately narrow:

- preparation is explicit;
- authoritative byte ownership is immutable;
- matching state remains per-call and ephemeral;
- one prepared input may safely support concurrent independent matches;
- the compiled-expression interface remains unchanged; and
- public text preparation, pooling, mutable cursors, and matcher internals remain out of scope.

The next tranche is **R2.4 — complex-pattern, cancellation, malformed-input, and resource-limit closure**. Icod.Grep PR #12 remains frozen until R2.5 provides a consumable CommandFramework 2.2.0 prerelease package for real cross-repository validation.
