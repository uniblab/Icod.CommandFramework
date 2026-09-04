# Icod.CommandFramework 2.2.0 — Post-R2.2 Whole-Suite Physical Report

**Purpose:** decide whether R2.3 decode/prepared-input work remains justified after accepted R2.1 and R2.2 optimizations  
**Baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Candidate:** `f29cd479b9f2ec65af04a89a837478e8991a883e`  
**Benchmark mode:** BenchmarkDotNet `InProcess`  
**Filter:** `*`  
**Passes:** 2, ABBA ordering  
**Cooldown:** 30 seconds  
**Hardware inventory SHA-256:** `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`  
**Decision:** R2.3 is justified; decode/materialization and repeated input preparation are now the dominant residual costs

## Executive conclusion

The post-R2.2 whole-suite measurement confirms that R2.1 and R2.2 materially changed the engine cost profile.

Repeated unanchored start attempts and deterministic sequence collection allocation are no longer the dominant costs on the targeted workloads. The remaining large allocations are now concentrated in full-input decode/materialization and repeated public searches that prepare the same authoritative input repeatedly.

R2.3 should therefore proceed as a **decode/prepared-input investigation**, with the narrowest safe goal being reuse of one prepared authoritative input across repeated matches. A public prepared-input API is not assumed; an internal reusable representation should be preferred unless consumer evidence later requires a public surface.

## Principal residual costs

### Large anchored byte input

The anchored controls isolate input preparation because they perform only one match start.

| Scenario | 2.1.0 allocation | Post-R2.2 allocation | Allocation change | Time change |
| --- | ---: | ---: | ---: | ---: |
| `bre-anchored-256k` | ~4.513 MiB | ~4.509 MiB | -0.08% | -8.3% |
| `bre-anchored-2m` | ~36.007 MiB | ~36.005 MiB | -0.01% | -10.7% |
| `bre-utf8-anchored-256k` | ~3.384 MiB | ~3.382 MiB | -0.07% | -12.9% |

These allocation deltas are effectively zero relative to the R2.0 allocation noise floor. The large-input decode/materialization cost therefore remains intact after R2.1/R2.2.

### Repeated search over one record

The repeated-search benchmark remains the strongest R2.3 signal:

| Scenario | 2.1.0 | Post-R2.2 | Change |
| --- | ---: | ---: | ---: |
| Mean time | ~88.44 ms | ~52.48 ms | -40.7% |
| Allocated | ~101.29 MiB | ~73.30 MiB | -27.6% |

R2.1/R2.2 substantially improve repeated search because matching after preparation is cheaper, but approximately **73.3 MiB** remains allocated per benchmark operation. The input record is only 64 KiB, so repeated preparation is now disproportionately expensive.

## R2.1/R2.2 gains retained

The whole-suite run independently confirms the accepted optimization results.

Representative literal-search changes versus 2.1.0:

- 256 KiB BRE literal miss: approximately **96.1% lower allocation** and **89–91% lower time**;
- 256 KiB BRE literal end-hit: approximately **96.1% lower allocation** and **90.0% lower time**;
- 4 KiB literal middle-hit: approximately **92.5% lower allocation** and **92.7% lower time**;
- 256 KiB ERE UTF-8 miss: approximately **94.3% lower allocation** and **91.3% lower time**.

Representative structural changes:

- BRE/ERE alternation: approximately **39.6% lower allocation**;
- BRE word-boundary: approximately **51.3% lower allocation**;
- BRE capture/backreference: approximately **25.3% lower allocation**;
- anchored ERE: approximately **60.2% lower allocation**;
- ERE optional/repetition: approximately **85.2% lower allocation**.

This confirms that the residual decode cost is not an artifact of failed R2.1/R2.2 optimization; those optimizations remain effective.

## Small regressions and non-priorities

Compilation allocations increased by roughly 15–33% on the measured compile scenarios, but absolute allocations remain around one to one-and-a-half KiB per compile. Compilation was already shown by R2.0 to be negligible for the Grep performance problem and remains a non-priority.

Several unchanged-allocation structural controls have modest timing movement, including bracket class and empty match. Their allocation is essentially identical to baseline, and the absolute costs are much smaller than the residual decode/repeated-search costs. They do not justify reordering the roadmap ahead of R2.3.

## R2.3 design direction

R2.3 should begin with a deliberately narrow investigation:

1. identify the internal representation currently produced from string/byte input before state matching;
2. separate authoritative input preparation from one match invocation without changing public match coordinates or malformed-input behavior;
3. allow repeated searches over the same prepared representation internally;
4. preserve exact byte boundaries, UTF-16 indices, malformed UTF-8 policies, locale provider behavior, cancellation, and thread safety;
5. avoid pooling or shared mutable buffers until ownership/lifetime rules are proven simple;
6. preserve the existing public `Match` APIs by having them prepare once and delegate to the prepared-input matcher; and
7. add a focused benchmark that compares repeated public preparation against repeated matching over one prepared input.

The first R2.3 candidate should target **internal reuse only**. A public prepared-input API should be considered only if Icod.Grep or another real consumer cannot obtain the measured benefit through existing higher-level command integration.

## R2.3 acceptance gate

A first candidate should be retained only if:

- repeated-search allocation drops materially below the current ~73.3 MiB result;
- large single-match anchored allocation does not regress;
- exact string/byte coordinate contracts remain unchanged;
- UTF-8 PreserveBytes/Replace/Throw behavior remains unchanged;
- cancellation and `MaximumMatchStates` behavior remain unchanged;
- compiled expressions remain reusable and thread-safe under the existing contract; and
- the full CommandFramework test/CI suite remains green.

After a successful focused candidate, repeat the whole-suite physical comparison before moving to R2.4.
