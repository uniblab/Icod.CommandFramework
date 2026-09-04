# Icod.CommandFramework 2.2.0 — R2.2 Candidate 1

**Tranche:** R2.2 — deterministic state-path allocation reduction  
**Implementation head:** `122510596fcc2f4940d48cd9d2779a11e7b78513`  
**Physical measurement head:** `9702c11576e3171180df6e1a39e2407fc8c47bdb`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Status:** physically validated and accepted; useful but deliberately bounded

## Scope

Candidate 1 deliberately avoids a general AST/state-engine rewrite.

`LiteralPrefixCompiledRegularExpression` already proves when a compiled expression is a complete, capture-free plain literal. R2.1 used that proof only for unanchored search; `RequireMatchAtStart=true` still delegated to the general matcher, which decoded the input and then traversed the full `RegexMatchState` / `SequenceRegexNode` path.

Candidate 1 extends the already-proven complete-literal specialization to anchored matching.

For complete literals with the default unlimited `MaximumMatchStates`, anchored text and byte matching now:

1. decodes the authoritative input once;
2. validates the requested start boundary exactly;
3. compares the complete literal directly at that decoded position using the configured character-class provider;
4. rejects opaque malformed preserved-byte units as literal candidates;
5. constructs the public match directly from source boundaries; and
6. returns zero captures, which is valid because complete-literal eligibility requires `CaptureCount == 0`.

## Explicit non-goals

Candidate 1 does **not** accelerate:

- structured required-prefix expressions;
- captures or backreferences;
- alternation or repetition;
- classes or assertions;
- any expression with finite `MaximumMatchStates`.

Those cases continue through the existing matcher unchanged.

Finite resource limits never receive the wrapper because `LiteralPrefixCompiledRegularExpression.Create` returns the inner expression whenever `MaximumMatchStates != int.MaxValue`. Resource-limit state accounting is therefore unchanged by construction.

## Semantic and CI coverage

The existing R2.1 tests cover anchored failure behavior, finite resource-limit fallback, syntax-profile equivalence, ignore-case character-provider semantics, malformed UTF-8 PreserveBytes behavior, and nonliteral fallback.

Candidate 1 adds focused tests for successful anchored complete literals at nonzero text and byte offsets.

Canonical Staging workflow **33830752966** passed on physical-measurement head `9702c11576e3171180df6e1a39e2407fc8c47bdb`, including the full CommandFramework suite and benchmark validation on Windows, Linux, and macOS.

## Physical comparison

The physical comparison used:

- baseline `460732c9f0cacb194bc6cd97c71612c492603eb6`;
- candidate `9702c11576e3171180df6e1a39e2407fc8c47bdb`;
- BenchmarkDotNet `InProcess` mode;
- filter `*RegexDecodeBenchmarks*`;
- two passes in ABBA order;
- 30-second cooldowns;
- the physical Windows reference laptop;
- hardware inventory SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`;
- BenchmarkDotNet `0.15.8` on .NET `10.0.11`; and
- all 8 decode/anchored workloads in each pass.

Two-pass means for the complete-literal targets were:

| Scenario | 2.1.0 mean | Candidate 1 mean | Time change | 2.1.0 allocated | Candidate 1 allocated | Allocation change |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `bre-anchored-80` | 1.439 µs | 0.569 µs | -60.5% | 4.31 KiB | 1.80 KiB | -58.2% |
| `bre-anchored-4k` | 22.076 µs | 21.953 µs | -0.6% | 74.92 KiB | 72.42 KiB | -3.3% |
| `bre-anchored-256k` | 2.207 ms | 2.151 ms | -2.5% | 4622.43 KiB | 4619.64 KiB | -0.06% |
| `bre-anchored-2m` | 15.496 ms | 17.105 ms | +10.4% | 36871.43 KiB | 36869.46 KiB | -0.01% |
| `bre-utf8-anchored-4k` | 15.311 µs | 13.432 µs | -12.3% | 56.95 KiB | 54.44 KiB | -4.4% |
| `bre-utf8-anchored-256k` | 1.767 ms | 1.667 ms | -5.7% | 3463.63 KiB | 3461.12 KiB | -0.07% |

The two malformed-input controls use pattern `.` rather than a complete literal and therefore remain on the general matcher. Their measured allocation is byte-for-byte unchanged at 72.64 KiB in both variants.

## Interpretation

Candidate 1 identifies a fixed deterministic-matcher allocation component of roughly **2.5 KiB per call** on the measured short/medium complete-literal shapes.

That fixed cost is significant for small inputs: on the 80-byte workload it is more than half of total allocation and removing it also produces a repeatable ~60% time reduction.

As input grows, decode/materialization dominates total allocation. The same fixed saving becomes only ~3–4% at 4 KiB and less than 0.1% at 256 KiB and 2 MiB. This is consistent with the R2.0 finding that large-input decode cost is linear and remains substantial after search-start optimization.

The 2 MiB two-pass mean is about 10% slower, but the individual passes disagree materially and the established physical-host timing spread is roughly 9.5%. The fallback controls also move several percent in elapsed time while retaining exactly identical allocation. This evidence is insufficient to claim a large-input regression; allocation remains the stronger signal.

## Decision

Candidate 1 is retained.

It is a low-risk specialization built on an eligibility proof already required by R2.1, produces a large improvement on short anchored literals, and removes a measurable fixed state-machine cost without changing finite resource accounting.

However, Candidate 1 does **not** close R2.2. Its large-input result confirms that the next useful question is broader deterministic sequence machinery rather than further complete-literal special cases.

The next candidate should optimize `SequenceRegexNode` only for sequences whose nodes are provably single-successor and non-capturing, while preserving the existing general collection path for branching/capturing structures and all finite `MaximumMatchStates` configurations.
