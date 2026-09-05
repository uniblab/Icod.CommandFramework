# Icod.CommandFramework 2.2.0 — R2.4 Closure

**Tranche:** R2.4 — complex-pattern, cancellation, malformed-input, and resource-limit closure  
**Predecessor:** R2.3 accepted and closed  
**Status:** semantic closure validation in progress

## Purpose

R2.4 is deliberately not a broad new optimization tranche. R2.1 through R2.3 have already changed start-position dispatch, deterministic state propagation, and repeated byte-input preparation. Before packaging those changes for Icod.Grep, R2.4 proves that difficult workloads still preserve the ordinary GNU matching contract through both the traditional one-shot byte API and the new public immutable prepared-input API.

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

## Acceptance gate

R2.4 closes if:

1. the new cross-path matrix is green on Windows, Linux, and macOS;
2. the complete existing CommandFramework test suite remains green;
3. benchmark project builds/smokes and collector validation remain green;
4. finite state exhaustion returns `MatchResourceLimitExceeded` identically through ordinary and prepared paths;
5. malformed-input policy and exact source coordinates remain unchanged;
6. cancellation remains an `OperationCanceledException` contract rather than a controlled regex diagnostic;
7. complex patterns that cannot use start-prefix acceleration retain ordinary general-matcher semantics; and
8. no production-code change is required unless the closure matrix exposes a real regression.

## Decision discipline

If the matrix passes without production changes, that is a positive result: R2.4 is intended to validate the accepted architecture rather than create another optimization merely to justify the tranche.

If a mismatch is found, fix the smallest proven semantic defect and add a regression case before closing R2.4.

After R2.4 closes, proceed to R2.5: produce a consumable CommandFramework 2.2.0 prerelease package and integrate it into the frozen Icod.Grep 1.6.0 performance branch for full GNU grep conformance and physical command-level measurement.
