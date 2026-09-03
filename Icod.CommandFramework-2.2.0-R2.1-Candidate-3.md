# Icod.CommandFramework 2.2.0 — R2.1 Candidate 3

**Tranche:** R2.1 — conservative required-prefix acceleration  
**Candidate 3 measurement head:** `a66fc947daf58fca4f2725c015e6a4c00853e72e`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Status:** literal-regression gate passed; structured-prefix physical measurement pending

## Candidate 3 scope

Candidate 3 extends the R2.1 wrapper beyond complete plain-literal expressions by deriving a conservative required literal prefix from supported AST shapes.

The optimization remains intentionally one-sided: when a prefix cannot be proven mandatory for every possible match, the expression falls back to the unchanged general matcher.

The analyzer does not infer through ambiguous alternation, opaque escapes, arbitrary classes, or other structures without a proof of required-prefix semantics. In particular, bounded BRE repetition remains a fallback control rather than an accelerated case.

## Physical literal-regression gate

The supplied physical comparison archive used:

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

Candidate-2-versus-Candidate-3 elapsed-time comparisons are not used to claim a regression or improvement because the physical timing spread is materially noisier than allocation. The correct conclusion from this run is narrower: Candidate 3 retains the proven literal-search optimization.

## Remaining Candidate 3 quantitative gate

This archive does **not** directly quantify the new structured required-prefix path because `RegexLiteralSearchBenchmarks` contains only the direct-search scenarios.

The next physical comparison should therefore use:

```text
*RegexStructuralBenchmarks*
```

The existing catalog already provides useful Candidate 3 coverage:

- `ere-repetition` — `TAR(GE)+T`; expected to exercise a provable leading literal prefix;
- `ere-optional` — `TAR(GE)?T`; expected to exercise a provable leading literal prefix;
- `bre-bounded-repetition` — conservative fallback control;
- alternation, bracket-class, word-boundary, capture/backreference, anchored, and empty-match cases — semantic/fallback controls.

The structured comparison should retain the same pinned baseline, physical host, `InProcess` mode, two-pass ABBA ordering, and 30-second cooldown discipline.

## Decision gate

If the structured-prefix workloads show a clear reduction in allocation and/or elapsed time materially larger than the baseline noise floor, while fallback controls remain semantically stable, Candidate 3 can close R2.1 and the project can proceed to R2.2 deterministic state-path allocation reduction.

If the structured-prefix workloads do not improve materially, retain Candidate 2's complete-literal optimization and reconsider whether Candidate 3's added analyzer complexity is justified before moving forward.
