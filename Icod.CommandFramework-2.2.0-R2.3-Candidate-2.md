# Icod.CommandFramework 2.2.0 — R2.3 Candidate 2

**Tranche:** R2.3 — immutable prepared-input consumer surface  
**Foundation:** R2.3 Candidate 1 accepted  
**Status:** public API implementation and CI validation in progress

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

When they receive a third-party `ICompiledRegularExpression` implementation that does not expose the internal prepared matcher, they fall back to that implementation's existing byte `Match` / `MatchAsync` methods using the immutable source snapshot and captured input options. This preserves correctness and compatibility without requiring external implementations to understand CommandFramework internals.

## Immutability and parallel execution

`RegularExpressionPreparedByteInput`:

- copies caller-owned authoritative bytes once during preparation;
- never exposes a mutable reference to that private snapshot;
- captures immutable `RegularExpressionInputOptions`;
- owns the immutable decoded/source-boundary representation used by native CommandFramework matching;
- stores no mutable match context, capture state, traversal state, or resource counter; and
- may therefore be reused concurrently by independent match calls without locks.

Returned byte match/capture values remain isolated from the prepared source, preserving the Candidate 1 mutation-safety contract.

## Benchmark gate

The candidate-only prepared-input benchmark has been changed to use only the **public** Candidate 2 API. The benchmark project no longer requires `InternalsVisibleTo` access.

The two methods remain:

- `PublicMatchLoop`: ordinary public `Match` calls that prepare the same 64 KiB record repeatedly;
- `PreparedMatchLoop`: public `RegularExpressionPreparedByteInput` plus the public prepared-input `Match` extension.

This ensures the next physical measurement proves the performance available to a real external consumer rather than an internal friend assembly.

## Acceptance gate

Candidate 2 should be retained if:

1. canonical Windows/Linux/macOS CI and the complete test suite remain green;
2. the public prepared-input benchmark smoke produces the same match count as the ordinary public path;
3. a physical two-pass run through the public API retains essentially the Candidate 1 reuse result;
4. ordinary public `ICompiledRegularExpression` callers remain unchanged;
5. external `ICompiledRegularExpression` implementations retain a correct fallback path; and
6. no mutable cursor, cache, pooling contract, or matcher-internal type becomes public.

After acceptance, CommandFramework R2.3 can close with a package-ready immutable consumer contract. Icod.Grep should consume that contract only when a 2.2.0 prerelease package is available, so its frozen PR branch never becomes intentionally unbuildable.
