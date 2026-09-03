# Icod.CommandFramework 2.2.0 — R2.1 Candidate 2

**Tranche:** R2.1 — conservative start-position acceleration  
**Candidate 1 measurement head:** `e4212f3ce0d81aaef70c59b78b7bd2fc01fa1777`  
**Candidate 2 implementation head:** `cc17fb83dc10ae587c767c116cdc5c09d53c7dc8`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Status:** Candidate 1 physically validated; Candidate 2 removes duplicate decode and awaits CI/physical confirmation

## Candidate 1 physical result

The focused physical comparison used:

- BenchmarkDotNet `InProcess` mode;
- filter `*RegexLiteralSearchBenchmarks*`;
- two ABBA passes with 30-second cooldowns;
- the same physical Windows reference laptop as R2.0;
- hardware inventory SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`; and
- 9/9 literal-search workloads executed in every run.

Candidate 1 produced very large improvements on the intended unanchored literal workloads.

| Scenario | 2.1.0 mean | Candidate 1 mean | Time change | 2.1.0 allocated | Candidate 1 allocated | Allocation change |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `bre-one-char-4k-miss` | 259.67 µs | 37.10 µs | -85.7% | 584.67 KiB | 72.37 KiB | -87.6% |
| `bre-literal-4k-middle` | 348.77 µs | 56.22 µs | -83.9% | 971.14 KiB | 147.28 KiB | -84.8% |
| `bre-literal-256k-end` | 42.08 ms | 5.79 ms | -86.2% | 116.51 MiB | 9.03 MiB | -92.3% |
| `bre-literal-256k-miss` | 38.27 ms | 3.38 ms | -91.2% | 116.51 MiB | 4.51 MiB | -96.1% |
| `ere-literal-256k-miss` | 45.07 ms | 3.49 ms | -92.3% | 116.51 MiB | 4.51 MiB | -96.1% |
| `bre-long-literal-256k-miss` | 42.98 ms | 3.41 ms | -92.1% | 116.51 MiB | 4.51 MiB | -96.1% |
| `bre-utf8-4k-middle` | 176.26 µs | 36.93 µs | -79.0% | 505.06 KiB | 111.33 KiB | -78.0% |
| `ere-utf8-256k-miss` | 30.02 ms | 2.13 ms | -92.9% | 59.38 MiB | 3.38 MiB | -94.3% |

The complete-miss workloads collapse essentially to the anchored decode/materialization floor established by R2.0. This confirms that repeated full matcher invocation at candidate start positions was the dominant first-order cost for plain literals.

## Candidate 1 residual inefficiency

`bre-literal-80-start` regressed from approximately 1.81 µs / 4.31 KiB to 2.42 µs / 6.06 KiB. The end-hit case also remained at about 9.03 MiB rather than the ~4.5 MiB anchored floor.

The cause is architectural rather than semantic: Candidate 1's wrapper decodes once to locate a candidate and then invokes the existing matcher, which decodes the same authoritative source again before performing its anchored match.

This duplicate decode is unnecessary because Candidate 1 only wraps expressions whose **entire pattern** is a provable plain literal sequence. Once the candidate scan has compared every decoded unit in the literal sequence successfully, the complete expression is already known to match.

## Candidate 2 refinement

Candidate 2 keeps the same deliberately narrow eligibility rules but no longer re-enters the general matcher after a successful candidate scan.

For eligible unanchored plain literals it now:

1. decodes the source once;
2. maps the requested start offset exactly;
3. scans left-to-right for the literal sequence using the configured character-class provider;
4. rejects opaque preserved-malformed units as literal candidates;
5. constructs the public match directly from the already-decoded source boundaries; and
6. returns zero captures, which is valid because eligibility additionally requires `CaptureCount == 0`.

`RequireMatchAtStart=true` still bypasses the accelerator and uses the untouched general matcher. Finite `MaximumMatchStates` still disables the accelerator so resource-limit state accounting remains exactly compatible with 2.1.0.

## Expected Candidate 2 outcome

Candidate 2 should preserve Candidate 1's large miss improvements while removing the extra decode on successful matches.

Expected allocation behavior:

- `bre-literal-80-start` should return close to the original anchored allocation floor rather than Candidate 1's regression;
- `bre-literal-4k-middle` should drop further because only one source decode remains;
- `bre-literal-256k-end` should move much closer to the ~4.5 MiB anchored byte-mode floor; and
- complete-miss cases should remain essentially unchanged from Candidate 1 because they already performed only one decode.

No broader AST-prefix inference is added in Candidate 2. Structured expressions such as `TARGET.*foo`, groups, alternation, assertions, repetition, classes, and backreferences continue through the existing matcher.
