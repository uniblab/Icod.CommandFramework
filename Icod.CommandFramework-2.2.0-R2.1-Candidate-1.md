# Icod.CommandFramework 2.2.0 — R2.1 Candidate 1

**Tranche:** R2.1 — conservative start-position acceleration  
**Candidate implementation head:** `84908c912abc4f3ccad35588aec3a9407d946eac`  
**Canonical Staging validation:** workflow run `33715219346` — green  
**Status:** semantic/CI validation complete; physical quantitative comparison pending

## Scope

Candidate 1 deliberately accelerates only expressions whose complete pattern is provably a plain literal sequence.

Examples that qualify:

```text
TARGET
abc
literal-text
```

Examples that deliberately fall back to the existing general matcher:

```text
TARGET.*foo
TAR[[:upper:]]ET
\(TARGET\)
(TARGET)
TARGET\|OTHER
TARGET|OTHER
^TARGET
TARGET$
```

The implementation is intentionally narrower than the eventual R2.1 roadmap ceiling. It creates a clean first measurement checkpoint against the R2.0 evidence before adding more compile-time AST inference.

## Implementation

The three GNU providers still compile the existing managed AST and matcher first. When the original pattern contains only Unicode scalar literals and `MaximumMatchStates` is left at its default unlimited value, the compiled expression is wrapped by an internal literal-prefix accelerator.

For an unanchored search the wrapper:

1. decodes the authoritative source using the same `RegexInput` implementation and input policy as the general matcher;
2. maps the caller's exact UTF-16 or byte start offset to a decoded-unit boundary;
3. scans left-to-right for the complete literal sequence without creating regex match-state objects;
4. compares candidate units through the configured `IRegularExpressionCharacterClassProvider`, preserving `IgnoreCase` semantics;
5. rejects opaque preserved-malformed units as literal candidates; and
6. invokes the existing matcher with `RequireMatchAtStart=true` at the first possible candidate.

A complete miss returns a successful no-match result after the allocation-free candidate scan without invoking the general state-machine path at every decoded unit.

`RequireMatchAtStart=true` bypasses the accelerator and retains the existing anchored path.

## Resource-limit contract

Candidate-start skipping would change the number of internal states observed by `MaximumMatchStates`. To preserve the 2.1.0 resource-limit contract exactly, the accelerator is disabled whenever `MaximumMatchStates` is finite. Such expressions use the unchanged general matcher.

This is conservative by design. R2.4 may revisit resource accounting only if a later optimization can preserve it explicitly.

## Tests

Focused tests cover:

- leftmost literal selection;
- nonzero `StartIndex`;
- case-insensitive character-provider comparison;
- UTF-8 preserved malformed bytes before a valid literal;
- anchored `RequireMatchAtStart` behavior;
- fallback for a nonliteral pattern;
- finite `MaximumMatchStates` behavior; and
- Basic, Extended, and Emacs plain-literal equivalence.

The complete repository suite and benchmark smoke passed in canonical PR workflow run `33715219346` on Windows, Linux, and macOS. Windows additionally passed the real in-process BenchmarkDotNet harness validation and pinned 2.1.0 collector smoke.

## Quantitative gate

R2.0 established these untouched-2.1.0 reference points:

- `bre-literal-256k-miss`: approximately **116.5 MiB** allocated;
- `ere-literal-256k-miss`: approximately **116.5 MiB** allocated;
- `bre-long-literal-256k-miss`: approximately **116.5 MiB** allocated;
- `bre-literal-256k-end`: approximately **116.5 MiB** allocated; and
- anchored 256 KiB byte control: approximately **4.5 MiB** allocated.

Candidate 1 should reduce the plain-literal unanchored workloads by an amount vastly larger than the R2.0 ~0.03% allocation noise floor. Timing is secondary because the reference host showed roughly 9.5% median two-pass timing spread.

The next physical comparison can be restricted to the literal benchmark class:

```powershell
powershell .\benchmarks\Collect-RegexReferenceComparison.ps1 `
    -Filter '*RegexLiteralSearchBenchmarks*'
```

If the measured allocation reduction is substantial and semantics remain green, the next R2.1 decision is whether to extend compile-time prefix derivation through safe AST shapes such as `TARGET.*foo`, or move directly to R2.2 deterministic state-path allocation reduction and remeasure first.
