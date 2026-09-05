# Icod.CommandFramework 2.2.0 — R2.4 Closure

**Tranche:** R2.4 — complex-pattern, cancellation, malformed-input, and resource-limit closure  
**Predecessor:** R2.3 accepted and closed  
**Status:** accepted and closed

## Purpose

R2.4 is deliberately not a broad new optimization tranche. R2.1 through R2.3 changed start-position dispatch, deterministic state propagation, and repeated byte-input preparation. Before packaging those changes for Icod.Grep, R2.4 proves that difficult workloads still preserve the ordinary GNU matching contract through both the traditional one-shot byte API and the new public immutable prepared-input API.

The principal acceptance question is:

> For inputs that force the general matcher, resource accounting, malformed-input handling, capture state, or zero-length behavior, does public prepared reuse remain semantically indistinguishable from ordinary public byte matching?

## Existing coverage retained

The pre-R2.4 suite already covers the individual mechanisms needed by this tranche, including:

- BRE and ERE `MaximumMatchStates` controlled failures;
- cancellation during compilation and matching;
- malformed UTF-8 `PreserveBytes`, `Replace`, and `Throw` behavior;
- exact UTF-8 source-boundary validation;
- captures and backreferences;
- repeated captures;
- word/anchor assertions;
- public prepared-input ownership and concurrent reuse;
- adversarial third-party prepared-input fallback isolation; and
- GNU Basic, Extended, and Emacs prepared matching.

R2.4 therefore adds a focused **cross-path parity matrix** rather than duplicating every preexisting unit test.

## Added closure matrix

`tests/CommandFramework.Tests/src/RegularExpressions/R2_4RegularExpressionClosureTests.cs` compares ordinary public byte matching against public immutable prepared-input matching across:

1. nested/branching BRE and ERE such as `(a|aa)*b`;
2. capture/backreference repetitions;
3. mixed alternation/repetition structures;
4. zero-length `a*` matches from a nonzero start offset;
5. finite `MaximumMatchStates` exhaustion;
6. malformed UTF-8 under `PreserveBytes` and `Replace`;
7. malformed UTF-8 `Throw` behavior during ordinary decode and prepared construction;
8. cancellation of complex prepared searches; and
9. bracket/class patterns for which required-prefix acceleration is unavailable.

For successful matches, the parity assertion compares:

- success/no-match state;
- diagnostic code;
- source-byte match index and length;
- exact returned byte value;
- capture count;
- each capture's success state;
- each capture's source-byte index/length; and
- each capture's exact returned byte value.

## Canonical acceptance result

Canonical PR workflow **33936220502** completed successfully at R2.4 head `e345a41b368348c4dd9572f98a2985a2f6b9eba2`.

The workflow passed:

- Windows, Linux, and macOS restore/build/test;
- Staging package validation on the designated package host;
- ordinary regex benchmark restore/build/smoke on all three host families;
- prepared-input benchmark restore/build/smoke on all three host families;
- Windows BenchmarkDotNet execution and artifact validation for both benchmark suites;
- prepared-input comparison collector smoke; and
- pinned `2.1.0` comparison collector smoke.

No production regex change was required to make the R2.4 matrix pass.

## Acceptance decision

**R2.4 is accepted and closed.**

The closure result establishes that:

1. the new cross-path matrix is green on Windows, Linux, and macOS;
2. the complete existing CommandFramework suite remains green;
3. benchmark projects and collector validation remain green;
4. finite state exhaustion returns `MatchResourceLimitExceeded` identically through ordinary and prepared paths;
5. malformed-input policy and exact source coordinates remain unchanged;
6. cancellation remains an `OperationCanceledException` contract rather than a controlled regex diagnostic;
7. complex patterns outside the start-prefix optimization retain ordinary general-matcher semantics; and
8. the accepted R2.1–R2.3 architecture required no semantic repair during adversarial closure.

## R2.4 closure conclusion

The absence of a production-code change is the desired outcome. R2.4 was a proof tranche, not an optimization tranche, and it demonstrates that the public prepared-input path remains a semantic peer of ordinary byte matching even when the engine is forced into difficult general-matcher cases.

The next tranche is **R2.5 — prerelease package and Icod.Grep consumer validation**. The prerelease mechanism must preserve the repository's stable tagged-release rules while allowing the still-Draft performance branch to be consumed by Grep before merge.
