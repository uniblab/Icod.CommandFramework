# Icod.CommandFramework 2.2.0 — R2.1 Candidate 3

**Tranche:** R2.1 — conservative required-prefix acceleration  
**Candidate 3 implementation head:** `a66fc947daf58fca4f2725c015e6a4c00853e72e`  
**Candidate 3 structural measurement head:** `bcfb97b5eb28ae5dee4ef749f17464038912feab`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Status:** physically validated and accepted; R2.1 complete

## Candidate 3 scope

Candidate 3 extends the R2.1 wrapper beyond complete plain-literal expressions by deriving a conservative required literal prefix from supported AST shapes.

The optimization remains intentionally one-sided: when a prefix cannot be proven mandatory for every possible match, the expression falls back to the unchanged general matcher.

The analyzer does not infer through ambiguous alternation, opaque escapes, arbitrary classes, or other structures without a proof of required-prefix semantics. In particular, bounded BRE repetition remains a fallback control rather than an accelerated case.

## Physical literal-regression gate

The first Candidate 3 physical comparison used:

- baseline `460732c9f0cacb194bc6cd97c71612c492603eb6`;
- candidate `a66fc947daf58fca4f2725c015e6a4c00853e72e`;
- BenchmarkDotNet `InProcess` mode;
- filter `*RegexLiteralSearchBenchmarks*`;
- two passes in ABBA order;
- 30-second cooldowns;
- the physical Windows reference laptop;
- hardware inventory SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`; and
- all 9 literal-search workloads in each pass.

Candidate 3 preserves Candidate 2's allocation gains on every literal workload.

| Scenario | 2.1.0 mean | Candidate 3 mean | Speedup | 2.1.0 allocated | Candidate 3 allocated | Allocation reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `bre-literal-256k-end` | 42.66 ms | 3.69 ms | 11.6× | 116.51 MiB | 4.51 MiB | 96.1% |
| `bre-literal-256k-miss` | 40.04 ms | 3.40 ms | 11.8× | 116.51 MiB | 4.51 MiB | 96.1% |
| `bre-literal-4k-middle` | 293.73 µs | 33.33 µs | 8.8× | 971.13 KiB | 72.42 KiB | 92.5% |
| `bre-literal-80-start` | 1.51 µs | 0.70 µs | 2.2× | 4.31 KiB | 1.80 KiB | 58.2% |
| `bre-long-literal-256k-miss` | 42.46 ms | 3.21 ms | 13.2× | 116.51 MiB | 4.51 MiB | 96.1% |
| `bre-one-char-4k-miss` | 232.80 µs | 36.89 µs | 6.3× | 584.67 KiB | 72.37 KiB | 87.6% |
| `bre-utf8-4k-middle` | 140.89 µs | 17.13 µs | 8.2× | 505.06 KiB | 54.44 KiB | 89.2% |
| `ere-literal-256k-miss` | 39.46 ms | 3.02 ms | 13.1× | 116.51 MiB | 4.51 MiB | 96.1% |
| `ere-utf8-256k-miss` | 25.58 ms | 1.87 ms | 13.7× | 59.38 MiB | 3.38 MiB | 94.3% |

Allocation is effectively unchanged relative to Candidate 2: observed differences are within approximately 0.1%, far below the magnitude of the R2.1 improvements and consistent with the established allocation-stability characteristics of the harness.

Candidate-2-versus-Candidate-3 elapsed-time comparisons are not used to claim a regression or improvement because the physical timing spread is materially noisier than allocation. The conclusion from this first run is deliberately narrow: Candidate 3 retains the proven literal-search optimization.

## Physical structured-prefix gate

The second physical comparison used the same frozen baseline, reference host, `InProcess` mode, two-pass ABBA discipline, and 30-second cooldowns, with:

```text
*RegexStructuralBenchmarks*
```

Collection identity:

- baseline `460732c9f0cacb194bc6cd97c71612c492603eb6`;
- candidate `bcfb97b5eb28ae5dee4ef749f17464038912feab`;
- hardware inventory SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`;
- BenchmarkDotNet `0.15.8` on .NET `10.0.11`;
- 10/10 structural workloads executed in each pass.

The two structured expressions for which Candidate 3 proves the required literal prefix `TAR` show a large, repeatable reduction:

| Scenario | Pattern | 2.1.0 two-pass mean | Candidate 3 two-pass mean | Time reduction | 2.1.0 allocated | Candidate 3 allocated | Allocation reduction |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `ere-optional` | `TAR(GE)?T` | 282.69 µs | 68.45 µs | 75.8% | 1004.75 KiB | 148.89 KiB | 85.2% |
| `ere-repetition` | `TAR(GE)+T` | 296.93 µs | 63.77 µs | 78.5% | 1005.22 KiB | 149.36 KiB | 85.1% |

These reductions are vastly larger than the R2.0 allocation noise floor and are present in both candidate passes.

## Fallback-control result

All eight non-target structural controls retained exactly the same measured allocation as `2.1.0`:

| Scenario | 2.1.0 allocated | Candidate 3 allocated | Runtime eligibility |
| --- | ---: | ---: | --- |
| `bre-alternation` | 2508.53 KiB | 2508.53 KiB | fallback |
| `bre-bounded-repetition` | 971.81 KiB | 971.81 KiB | fallback |
| `bre-bracket-class` | 1258.51 KiB | 1258.51 KiB | fallback |
| `bre-capture-backreference` | 1963.23 KiB | 1963.23 KiB | fallback |
| `bre-empty-match` | 2.57 KiB | 2.57 KiB | fallback |
| `bre-word-boundary` | 972.45 KiB | 972.45 KiB | fallback |
| `ere-alternation` | 2508.54 KiB | 2508.54 KiB | fallback |
| `ere-anchor` | 3.67 KiB | 3.67 KiB | fallback |

Inspection of `RequiredLiteralPrefixAnalyzer` confirms these are genuine fallback controls rather than merely workloads that happened not to improve: alternation is rejected, escaped BRE bounded repetition is rejected, classes/assertions/groups at the start produce no prefix, anchors produce no prefix, and `a*` is rejected because the leading literal is optional.

Elapsed-time values for several controls moved noticeably between passes, including in opposite directions. Because these controls execute the unchanged runtime path and their allocation is identical, those timing movements are treated as physical-host variance rather than optimization effects.

## R2.1 decision

Candidate 3 is accepted.

The combined R2.1 evidence now establishes that:

1. Candidate 2 removes the dominant repeated-start allocation for complete literals and removes Candidate 1's duplicate-decode penalty;
2. Candidate 3 preserves those literal gains;
3. Candidate 3 extends the improvement to conservatively provable structured prefixes with roughly 85% lower allocation on the measured ERE optional/repetition cases;
4. unsupported structures continue through the unchanged general matcher; and
5. the complete semantic/CI suite remains green on Windows, Linux, and macOS.

R2.1 is therefore complete. The next tranche is **R2.2 — deterministic state-path allocation reduction**.
