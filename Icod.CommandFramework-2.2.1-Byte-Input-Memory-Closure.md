# Icod.CommandFramework 2.2.1 — Byte-Input Memory Closure

**Baseline:** `3c7b8189f664be67b74fb71809580d3133dc135f` (`2.2.0`)  
**Candidate:** `e499bf01c0da0eb9134d5c494303f30e7bb8a56e`  
**Reference host:** physical Windows reference host, hardware hash `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`  
**Runtime:** .NET 10.0.11, x64, Concurrent Workstation GC  
**Benchmark:** `PreparedByteInputConstructionBenchmarks.PrepareByteMode1MiB`  
**Protocol:** two-pass ABBA comparison with 30-second cooldown  
**Status:** candidate accepted for 2.2.1

## 1. Motivation

Icod.Grep T6.8 physical stress testing exposed excessive managed allocation when a very large byte-mode record is prepared for the managed GNU BRE/ERE engine. A 64 MiB record caused roughly 1.54 GB of managed allocation in the Grep consumer path.

The dominant avoidable source was `RegexInput.Decode` for `TextDecodingMode.Bytes`. The previous implementation allocated full-capacity `List<Rune>`, `List<bool>`, and `List<int>` buffers and then copied all three collections into final arrays.

Because byte mode has an exact one-input-byte-to-one-matching-unit relationship, the final array lengths are known before decoding begins.

## 2. Candidate

For `TextDecodingMode.Bytes`, the candidate now allocates the final arrays directly:

- one `Rune[]` with `source.Length` elements;
- one `bool[]` with `source.Length` elements; and
- one source-index `int[]` with `source.Length + 1` elements.

The arrays are filled in one pass. The UTF-8 decoder path is intentionally unchanged.

The change preserves:

- exact byte-to-unit mapping;
- source-coordinate mapping;
- byte-mode opaque-unit behavior;
- cancellation checks;
- malformed-UTF policy boundaries;
- matching and capture semantics; and
- the existing public API.

## 3. CI gate

PR workflow run 119 completed successfully on Windows, Linux, and macOS, including the existing regular-expression conformance tests, benchmark smoke paths, package validation, and comparison-orchestration checks.

After the Windows temporary-worktree path was shortened, workflow run 120 also completed successfully.

## 4. Physical result

The focused benchmark constructs one immutable prepared input from a 1 MiB authoritative byte array through the public `RegularExpressionPreparedByteInput.Prepare(...)` API.

| Run | 2.2.0 baseline | Candidate | Candidate / baseline | Change |
| --- | ---: | ---: | ---: | ---: |
| Pass 1 | 13.01 ms | 4.242 ms | 0.326 | **-67.39%** |
| Pass 2 | 34.64 ms | 3.886 ms | 0.112 | **-88.78%** |
| Two-pass mean | 23.825 ms | 4.064 ms | 0.171 | **-82.94%** |

The second baseline pass had visibly higher variance, so the two-pass timing mean should not be interpreted as a precise speedup estimate. The important timing conclusion is stronger and simpler: both candidate runs are materially faster than even the faster baseline run. There is no construction-time regression.

### Managed allocation

BenchmarkDotNet GC counters yield the following per-operation allocation:

| Run | 2.2.0 baseline | Candidate |
| --- | ---: | ---: |
| Pass 1 | 19.0047 MiB | 10.0042 MiB |
| Pass 2 | 19.0067 MiB | 10.0035 MiB |
| Mean | **19.0057 MiB** | **10.0038 MiB** |

That is a **47.36% reduction in managed allocation**, or approximately **9.00 MiB less allocation for every 1 MiB byte-mode prepared input**.

Allocation is extremely stable across both passes, unlike the noisier elapsed-time baseline, so this is the primary acceptance signal.

## 5. Acceptance

The candidate is accepted because:

- managed allocation falls by **47.36%** in the targeted public construction path;
- the candidate is materially faster in both physical passes;
- the direct-allocation representation is simpler than the previous list-plus-copy construction;
- byte-mode semantics are unchanged;
- UTF-8 decoding is untouched; and
- all cross-platform CI and conformance gates are green.

## 6. Release decision

This is a compatible implementation optimization and is appropriate for a patch release.

The next package version is **Icod.CommandFramework 2.2.1**.

After 2.2.1 is published, Icod.Grep should consume it and rerun only the T6.8 `records` profile on the same physical reference host. The consumer-level acceptance target is a substantial reduction from the approximately 23× allocation amplification previously observed for large BRE/ERE byte-mode records.
