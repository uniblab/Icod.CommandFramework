# Icod.CommandFramework 2.2.0 — R2.0 Physical Reference Baseline Report

**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Infrastructure candidate:** `2.2.0` branch at `c002701cd980e9772b74952075546ba147fcfa61`  
**Benchmark mode:** BenchmarkDotNet `InProcess`  
**Reference host:** physical Windows x64 laptop, AMD Ryzen 7 5700U family, 16 logical processors, .NET 10.0.11  
**Hardware inventory SHA-256:** `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`  
**Collection:** two ABBA passes, 30-second cooldown, 32 workloads per run  
**Result:** R2.0 quantitative gate satisfied

## 1. Collection integrity

The physical-reference archive is complete and suitable for the R2.0 gate.

`comparison.json` records:

- schema version `1`;
- immutable baseline commit `460732c9f0cacb194bc6cd97c71612c492603eb6`;
- candidate commit `c002701cd980e9772b74952075546ba147fcfa61`;
- `BenchmarkMode: InProcess`;
- filter `*`;
- `Smoke: false`;
- two passes;
- 30-second cooldowns;
- ABBA sequence `baseline → candidate → candidate → baseline`; and
- the exact hardware-inventory SHA-256 established by the preceding `Icod.Grep` T6 reference series.

All four BenchmarkDotNet runs discovered and executed all **32/32** workloads. No build-error, zero-benchmark, or benchmark-issue marker is present in the retained logs.

The per-pass metadata agrees on the relevant environment:

- Windows x64;
- x64 process architecture;
- .NET 10.0.11;
- 16 logical processors;
- workstation concurrent GC;
- interactive GC latency mode; and
- BenchmarkDotNet 0.15.8.

The candidate contains benchmark/CI/version infrastructure but no production regular-expression optimization. Candidate-versus-baseline timing differences therefore measure harness/host variability, not an intended engine speedup.

## 2. Noise floor

Allocation measurements are extremely stable while elapsed-time measurements show meaningful run-to-run variance.

Across all 32 workloads:

- candidate/baseline timing geometric mean: approximately **+2.0%**;
- candidate/baseline timing median: approximately **+1.8%**;
- median absolute candidate/baseline timing difference: approximately **6.0%**;
- 75th percentile absolute timing difference: approximately **11.3%**;
- median two-pass timing spread: approximately **9.8%** for baseline and **9.0%** for candidate;
- worst two-pass timing spread: approximately **37.8%** for baseline and **50.6%** for candidate;
- median allocation delta: effectively **0%**; and
- worst observed allocation delta: approximately **0.03%**.

Consequences for later tranches:

1. allocation is the strongest quantitative signal for R2.1/R2.2;
2. elapsed-time changes in the single-digit percentage range must not be presented as wins without stronger repeat evidence; and
3. intended optimizations should produce effects materially larger than the observed timing noise on their targeted workloads.

## 3. Untouched 2.1.0 baseline

The values below are the mean of the two untouched-2.1.0 physical passes. Timing is retained as orientation; allocation is the more stable discriminator.

### 3.1 Decode/setup controls

| Scenario | Mean | Allocated |
| --- | ---: | ---: |
| `bre-anchored-80` | 1.47 µs | 4.31 KiB |
| `bre-anchored-4k` | 22.57 µs | 74.92 KiB |
| `bre-anchored-256k` | 2.16 ms | 4.51 MiB |
| `bre-anchored-2m` | 15.58 ms | 36.01 MiB |
| `bre-utf8-anchored-4k` | 17.46 µs | 56.95 KiB |
| `bre-utf8-anchored-256k` | 2.48 ms | 3.38 MiB |
| `bre-invalid-preserve-4k` | 23.44 µs | 72.64 KiB |
| `bre-invalid-replace-4k` | 23.56 µs | 72.64 KiB |

Byte-mode anchored allocation scales almost perfectly with input length: approximately **18 bytes allocated per input byte** for the 256 KiB and 2 MiB controls. Full-input materialization is therefore real, linear, and non-trivial even before unanchored search begins.

### 3.2 Unanchored literal search

| Scenario | Mean | Allocated |
| --- | ---: | ---: |
| `bre-literal-80-start` | 1.41 µs | 4.31 KiB |
| `bre-one-char-4k-miss` | 179.79 µs | 584.67 KiB |
| `bre-literal-4k-middle` | 275.61 µs | 971.14 KiB |
| `bre-utf8-4k-middle` | 138.94 µs | 505.06 KiB |
| `bre-literal-256k-end` | 37.01 ms | 116.51 MiB |
| `bre-literal-256k-miss` | 38.21 ms | 116.51 MiB |
| `bre-long-literal-256k-miss` | 34.88 ms | 116.51 MiB |
| `ere-literal-256k-miss` | 41.73 ms | 116.51 MiB |
| `ere-utf8-256k-miss` | 28.57 ms | 59.38 MiB |

The contrast with the anchored controls isolates the cost of repeated start attempts:

- 4 KiB BRE literal middle hit versus 4 KiB anchored control: about **12.2×** elapsed time and **13.0×** allocation;
- 256 KiB BRE literal miss versus 256 KiB anchored control: about **17.6×** elapsed time and **25.8×** allocation;
- 256 KiB UTF-8 miss versus 256 KiB UTF-8 anchored control: about **11.5×** elapsed time and **17.6×** allocation.

The short and long 256 KiB literal misses allocate essentially the same **116.5 MiB**. Literal width is therefore not the first-order allocation driver for these misses. The repeated general matcher invocation at candidate start positions is.

The 256 KiB byte-mode miss allocates roughly **466 bytes per input byte**, compared with roughly **18 bytes per input byte** for anchored preparation. This is the clearest direct evidence for R2.1 start-position acceleration.

## 4. Structural state-machine cost

Representative 4 KiB structural workloads show additional allocation beyond simple literal search:

| Scenario | Mean | Allocated |
| --- | ---: | ---: |
| `bre-bounded-repetition` | 251.78 µs | 971.81 KiB |
| `ere-repetition` | 269.07 µs | 1005.22 KiB |
| `ere-optional` | 275.36 µs | 1004.75 KiB |
| `bre-word-boundary` | 303.29 µs | 972.45 KiB |
| `bre-bracket-class` | 483.45 µs | 1.23 MiB |
| `bre-capture-backreference` | 580.01 µs | 1.92 MiB |
| `bre-alternation` | 719.95 µs | 2.45 MiB |
| `ere-alternation` | 778.58 µs | 2.45 MiB |
| `ere-anchor` | 1.36 µs | 3.67 KiB |
| `bre-empty-match` | 0.88 µs | 2.57 KiB |

Alternation allocates about **2.5×** as much as the simple 4 KiB literal-middle workload, and the capture/backreference case about **2×** as much. This supports R2.2's separate investigation of state/list/hash-set/capture churn after R2.1 reduces impossible start attempts.

## 5. Repeated public search

`RegexRepeatedSearchBenchmarks.FindAll` uses one 64 KiB record with 64 deterministic `TARGET` markers and repeatedly calls the public `Match` API until the terminating miss.

Untouched 2.1.0 averages:

- approximately **85.44 ms**; and
- approximately **101.29 MiB** allocated per operation.

This workload is consistent with repeated full-input preparation remaining material when a caller searches the same record many times. It keeps R2.3 prepared-input/decode reuse justified, but the unanchored single-call multipliers above are larger and more direct, so R2.3 should remain after R2.1/R2.2 unless remeasurement changes the ordering.

## 6. Compilation is not a priority

Representative compilation costs are small:

| Scenario | Mean | Allocated |
| --- | ---: | ---: |
| `bre-literal` | 0.39 µs | 696 B |
| `bre-backreference` | 0.40 µs | 872 B |
| `ere-repetition` | 0.87 µs | 1.23 KiB |
| `ere-alternation` | 1.06 µs | 1.32 KiB |

Compilation is not responsible for the Grep-scale allocation problem and should not receive optimization priority in 2.2.0.

## 7. R2.0 conclusions

The physical series distinguishes the suspected costs sufficiently to close R2.0:

1. **Decode/materialization:** confirmed linear and significant, approximately 18 allocated bytes per byte for large byte-mode anchored controls.
2. **Unanchored start scanning:** confirmed as the first implementation target; it multiplies allocation by roughly 13× on a 4 KiB middle hit and 26× on a 256 KiB miss relative to anchored controls.
3. **Deterministic/general state machinery:** confirmed as a separate contributor; structural branching/capture workloads add substantial allocation beyond simple literal cases.
4. **Repeated caller search:** confirms that repeated full-input preparation can remain expensive and preserves the rationale for R2.3.
5. **Compilation:** negligible for the observed problem.

The planned tranche order remains appropriate:

1. R2.1 — conservative start-position / required-prefix acceleration;
2. R2.2 — deterministic state-path allocation reduction;
3. remeasure;
4. R2.3 — decode/prepared-input work if still justified;
5. R2.4 — complex-pattern/resource-limit closure; and
6. R2.5 — prerelease package and `Icod.Grep` consumer validation.

## 8. R2.1 measurement expectations

Because the timing harness has a roughly 10% median two-pass spread, R2.1 acceptance should emphasize allocation and direct search-attempt reduction.

For provable literal-prefix workloads, R2.1 should aim for:

- a large reduction in full-expression start attempts;
- allocation reductions far larger than the ~0.03% infrastructure noise floor;
- substantial improvement on 256 KiB end/miss workloads where the baseline allocates ~116.5 MiB per operation;
- no regression in anchored, empty-match, alternation, capture/backreference, malformed-input, cancellation, or resource-limit semantics; and
- end-to-end Grep confirmation only after the shared-engine direct benchmark shows a measured candidate.
