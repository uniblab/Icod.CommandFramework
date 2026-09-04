# Icod.CommandFramework 2.2.0 — Managed Regular-Expression Performance Roadmap

**Baseline:** `main` / `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Target release:** `2.2.0`  
**Consumer driver:** `Icod.Grep 1.6.0` T6 performance work  
**Theme:** reduce managed BRE/ERE search cost and allocation without changing regex semantics  
**Status:** R2.1 and R2.2 complete — whole-suite remeasurement next

## 1. Why 2.2.0 exists

`Icod.Grep 1.6.0` T6.0 established a controlled physical-reference benchmark suite and identified the shared managed regular-expression implementation as the first-order allocation hotspot for BRE/ERE workloads.

On the reference Windows laptop, representative Grep measurements showed:

- roughly **604.59 MB** allocated by a short-record BRE workload versus **6.38 MB** for the comparable record-reader control;
- roughly **3,005.83 MB** allocated by a long-record BRE workload versus **53.54 MB** for the comparable record-reader control;
- roughly **4,835.96 MB** allocated by the large-file BRE workload;
- fixed-string and PCRE workloads allocating only a few megabytes on their corresponding benchmark shapes.

The record-reader controls demonstrate that record materialization alone does not explain the managed allocation volume. Inspection of `Icod.CommandFramework.RegularExpressions` shows several plausible contributors:

- every byte-preserving match decodes the complete input into fresh `RegexInput` storage;
- unanchored search attempts the expression at successive input-unit start positions;
- each start constructs a fresh `RegexMatchState`;
- deterministic node advancement creates new states;
- `SequenceRegexNode` creates `List<RegexMatchState>` and `HashSet<RegexMatchState>` instances as it advances;
- capture mutations clone capture arrays; and
- the general state-machine path is used even for simple deterministic literal sequences.

Version `2.2.0` will optimize these costs in the shared regex provider so every Icod consumer benefits. `Icod.Grep` will remain a downstream acceptance and performance consumer; it will not grow a private BRE/ERE engine or semantic shortcut around CommandFramework.

## 2. Non-negotiable semantic contract

Performance work must preserve the complete public and tested regular-expression behavior of `2.1.0`, including:

- GNU Basic, GNU Extended, and GNU Emacs syntax behavior;
- leftmost match selection and longest-match behavior;
- capture numbering, capture spans, and backreferences;
- bracket expressions, ranges, named classes, equivalence classes, and collating elements within the documented provider contract;
- case-insensitive matching through the configured character-class provider;
- byte-preserving and string matching APIs;
- UTF-8 decoding modes and malformed-input policies;
- exact byte/source offsets and rejection of invalid split-boundary start positions;
- anchors and word-boundary assertions;
- empty matches and zero-length behavior;
- cancellation;
- `MaximumMatchStates` resource-limit diagnostics;
- controlled diagnostics and error codes; and
- thread-safe reuse of compiled expressions where currently supported.

No optimization is acceptable merely because it passes Grep's common cases. The complete CommandFramework regex suite remains the semantic authority.

## 3. Measurement discipline

The governing rule is:

> **Profile the shared engine directly, optimize the smallest proven bottleneck, then revalidate through real consumers.**

CommandFramework needs its own benchmark project rather than relying only on Grep's end-to-end measurements. Direct benchmarks separate:

- decode cost;
- start-position search cost;
- state-machine cost;
- capture/backreference cost; and
- caller/record-processing cost outside the regex engine.

BenchmarkDotNet infrastructure must remain outside the NuGet package and normal production assemblies.

Hosted CI benchmark execution is diagnostic/smoke evidence only. Narrow quantitative claims are made from controlled repeated measurements on the same physical reference laptop used by `Icod.Grep` T6.

## 4. R2.0 — Direct regex benchmark foundation

**Implementation status:** complete. The code-side harness and the quantitative physical-reference baseline are both closed.

The implemented non-packable project is `benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj`. It uses only public CommandFramework regular-expression APIs, remains outside `Icod.CommandFramework.sln`, and does not change production regex source.

Implemented benchmark groups are:

- `RegexLiteralSearchBenchmarks` — unanchored BRE/ERE search by literal width, input length, and hit position;
- `RegexDecodeBenchmarks` — `RequireMatchAtStart` controls that isolate full-input decode/setup from repeated unanchored start scanning;
- `RegexStructuralBenchmarks` — alternation, `+`, `?`, bounded repetition, brackets/classes, word boundaries, captures/backreferences, anchors, and empty matches;
- `RegexRepeatedSearchBenchmarks` — repeated public `Match` calls over one record, modeling consumers such as grep `-o`; and
- `RegexCompileBenchmarks` — representative BRE/ERE compilation cost independent of matching.

The deterministic smoke covers representative hits, misses, one-character search, byte and valid UTF-8 inputs, malformed UTF-8 Preserve/Replace behavior, malformed UTF-8 Throw behavior, and all structural scenarios.

PR CI builds and runs the existing 468-test CommandFramework suite plus the regex benchmark build/smoke on Windows, Linux, and macOS. Windows additionally exercises the pinned-`2.1.0` comparison collector under Windows PowerShell 5.1 and validates a real BenchmarkDotNet execution path.

The collector `benchmarks/Collect-RegexReferenceComparison.ps1`:

1. pins the immutable `2.1.0` baseline commit `460732c9f0cacb194bc6cd97c71612c492603eb6`;
2. creates fresh sibling detached worktrees for both baseline and candidate;
3. overlays the current benchmark harness into the baseline worktree so both variants execute identical benchmark code;
4. restores/builds each variant once;
5. performs two alternating passes in ABBA order by default;
6. waits 30 seconds between variants by default;
7. records each pass independently with BenchmarkDotNet artifacts and reproducibility metadata;
8. records the sibling `Icod.Grep/hardware_inventory.txt` SHA-256 when available, without copying its contents; and
9. uses BenchmarkDotNet in-process mode for physical collection so the authoritative path is not dependent on generated-project behavior from a particular .NET SDK patch level.

### R2.0 physical-reference result

The retained physical series is documented in [`Icod.CommandFramework-2.2.0-R2.0-Reference-Baseline-Report.md`](Icod.CommandFramework-2.2.0-R2.0-Reference-Baseline-Report.md).

Collection identity:

- baseline: `460732c9f0cacb194bc6cd97c71612c492603eb6`;
- infrastructure candidate: `c002701cd980e9772b74952075546ba147fcfa61`;
- mode: BenchmarkDotNet `InProcess`;
- two passes / ABBA ordering / 30-second cooldowns;
- exact reference-hardware SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`;
- 32/32 workloads executed in every pass.

Principal findings:

- allocation is extremely stable between baseline and infrastructure candidate; the worst observed allocation delta is about **0.03%**;
- timing is noisier, with a roughly **9.5%** median two-pass spread across the combined baseline/candidate series;
- large byte-mode anchored controls allocate about **18 bytes per input byte**, confirming linear decode/input materialization cost;
- a 256 KiB unanchored BRE literal miss allocates about **116.5 MiB**, versus about **4.5 MiB** for the corresponding anchored control — approximately a **26×** allocation multiplier;
- short and long 256 KiB literal misses allocate essentially the same amount, showing literal width is not the first-order allocation driver;
- a repeated public `Match` workload over one 64 KiB record allocates about **101 MiB**, preserving the rationale for later prepared-input/decode reuse;
- branching and capture/backreference structural workloads allocate materially more than simple deterministic cases; and
- compilation cost is negligible for the observed Grep problem.

### R2.0 conclusion

The physical series distinguishes the principal suspected contributors sufficiently to close the tranche:

1. decode/materialization is linear and significant;
2. unanchored start scanning is the largest directly isolated multiplier and therefore R2.1's first target;
3. general deterministic/branching state machinery remains a separate R2.2 target;
4. repeated public search keeps R2.3 justified after R2.1/R2.2 remeasurement; and
5. compilation is not an optimization priority.

## 5. R2.1 — Conservative start-position acceleration

**Status:** complete. Candidate 3 accepted.

R2.1 reduced the cost of unanchored search by proving where a match can begin before invoking the full state-machine matcher.

### Candidate progression

- **Candidate 1** accelerated only complete plain literals and demonstrated the expected collapse in repeated-start allocation, but decoded successful inputs twice.
- **Candidate 2** retained complete-literal eligibility and constructed successful matches directly from the already-decoded candidate scan, removing the duplicate decode.
- **Candidate 3** extended the same mechanism to conservatively provable required literal prefixes while preserving fallback to the unchanged general matcher whenever certainty is unavailable.

The retained evidence is documented in:

- [`Icod.CommandFramework-2.2.0-R2.1-Candidate-2.md`](Icod.CommandFramework-2.2.0-R2.1-Candidate-2.md); and
- [`Icod.CommandFramework-2.2.0-R2.1-Candidate-3.md`](Icod.CommandFramework-2.2.0-R2.1-Candidate-3.md).

### Physical acceptance result

Candidate 2's complete-literal path reduced representative allocation as follows:

- 256 KiB literal end-hit/miss: approximately **116.5 MiB → 4.5 MiB** (~96% lower);
- 4 KiB middle hit: approximately **971 KiB → 72 KiB** (~92.5% lower);
- 80-byte start hit: approximately **4.3 KiB → 1.8 KiB** (~58% lower).

Candidate 3 preserved those literal gains and then passed a dedicated structural-prefix gate. On `RegexStructuralBenchmarks`:

- `ere-optional` (`TAR(GE)?T`) fell from about **1004.75 KiB to 148.89 KiB** allocated (**85.2% lower**) and from about **282.69 µs to 68.45 µs** mean time (**75.8% lower**);
- `ere-repetition` (`TAR(GE)+T`) fell from about **1005.22 KiB to 149.36 KiB** allocated (**85.1% lower**) and from about **296.93 µs to 63.77 µs** mean time (**78.5% lower**).

All eight unsupported structural controls retained exactly the same measured allocation as `2.1.0`. Inspection of `RequiredLiteralPrefixAnalyzer` confirms those cases execute the unchanged fallback path, so control timing movement is treated as physical-host variance rather than an optimization effect.

### R2.1 conclusion

R2.1 is closed because the evidence establishes all of the intended properties:

1. the dominant repeated-start allocation for complete literals is materially reduced;
2. successful complete literals no longer pay a duplicate-decode penalty;
3. conservative structured prefixes receive the same start-position benefit;
4. unsupported patterns fall back to the original matcher; and
5. the full semantic/CI suite remains green across Windows, Linux, and macOS.

## 6. R2.2 — Deterministic state-path allocation reduction

**Status:** complete. Candidate 3 accepted.

R2.2 reduces allocation inside the general matcher by specializing only provably deterministic, non-capturing sequence paths.

### Candidate progression

- **Candidate 1** extended the already-proven complete-literal specialization to anchored matching and identified a fixed deterministic state-machine cost of roughly 2.5 KiB per call on the measured short/medium shapes.
- **Candidate 2** moved into `SequenceRegexNode`, carrying a single `RegexMatchState` through sequences whose direct children were proven single-successor and non-capturing. It produced substantial allocation reductions, including nested gains inside otherwise complex patterns, but two independent ABBA collections reproduced a roughly 35–42% timing regression on the unchanged-allocation `bre-bounded-repetition` fallback workload.
- **Candidate 3** retained Candidate 2's eligibility proof while splitting the deterministic and general collection algorithms into separate iterator methods. `SequenceRegexNode.Match` became a small non-iterator dispatcher.

The retained evidence is documented in:

- [`Icod.CommandFramework-2.2.0-R2.2-Candidate-1.md`](Icod.CommandFramework-2.2.0-R2.2-Candidate-1.md);
- [`Icod.CommandFramework-2.2.0-R2.2-Candidate-2.md`](Icod.CommandFramework-2.2.0-R2.2-Candidate-2.md); and
- [`Icod.CommandFramework-2.2.0-R2.2-Candidate-3.md`](Icod.CommandFramework-2.2.0-R2.2-Candidate-3.md).

### Accepted deterministic boundary

A sequence may use the optimized single-state path only when every direct child is one of:

- `EmptyRegexNode`;
- `LiteralRegexNode`;
- `DotRegexNode`;
- `AssertionRegexNode`;
- `CharacterClassRegexNode`; or
- `BracketRegexNode`.

The deterministic path is disabled whenever `MaximumMatchStates` is finite. Groups, alternation, repetition, backreferences, and future unproven node types therefore retain the general collection path at the containing sequence level. Nested deterministic child sequences may still optimize independently.

### Physical acceptance result

Candidate 3 passed canonical PR workflow `33859421589` and the authoritative physical `*RegexStructuralBenchmarks*` comparison on the same reference Windows host.

Representative two-pass results versus `2.1.0` were:

- `bre-alternation`: **39.6% lower allocation**, **32.9% lower mean time**;
- `ere-alternation`: **39.6% lower allocation**, **40.0% lower mean time**;
- `bre-word-boundary`: **51.3% lower allocation**, **38.1% lower mean time**;
- `bre-capture-backreference`: **25.3% lower allocation**, **23.0% lower mean time**;
- `ere-anchor`: **60.2% lower allocation**, **47.7% lower mean time**;
- `ere-optional`: **85.2% lower allocation**, **81.9% lower mean time**;
- `ere-repetition`: **85.2% lower allocation**, **79.8% lower mean time**.

Most importantly, `bre-bounded-repetition` returned to the baseline envelope and measured **9.3% faster than 2.1.0** while retaining exactly **971.81 KiB** allocation. `bre-bracket-class` and `bre-empty-match` also retained exactly baseline allocation.

### R2.2 conclusion

R2.2 is closed because:

1. deterministic sequence stages avoid per-stage `List<RegexMatchState>` / `HashSet<RegexMatchState>` allocation;
2. the optimized path is restricted to a compile-time-safe single-successor, non-capturing node set;
3. finite resource accounting remains on the general matcher;
4. nested deterministic sequences may benefit without changing outer branching/capture semantics;
5. Candidate 2's reproducible fallback timing regression is eliminated; and
6. the full semantic/CI suite remains green across Windows, Linux, and macOS.

## 7. R2.3 — Decode/prepared-input investigation

Each byte-oriented `Match` currently materializes a decoded `RegexInput`. Whether this remains a dominant cost after R2.1/R2.2 must be measured rather than assumed.

R2.0 confirms that the cost is real: large byte-mode anchored controls allocate about 18 bytes per input byte, and the repeated 64 KiB public-search workload allocates about 101 MiB. This is sufficient to keep R2.3 on the roadmap, but not sufficient to commit to implementation before the post-R2.2 whole-suite remeasurement.

Before R2.3 implementation begins, rerun the complete direct benchmark suite against the pinned `2.1.0` baseline with both accepted optimization tranches in place. The whole-suite result should determine the new dominant residual cost.

If decode/materialization remains significant after that remeasurement, evaluate options such as:

- internal prepared-input representation reused across repeated searches of the same record;
- cheaper byte-mode representation that preserves source mapping without unnecessary Rune-oriented structures;
- pooling where lifetime and stale-data guarantees are simple and safe; or
- a public prepared-input API only if multiple consumers have a real need and an internal solution is insufficient.

A public API addition is not a goal by itself. Prefer internal implementation improvement unless measurement demonstrates that caller-level reuse is required.

## 8. R2.4 — Complex-pattern and resource-limit audit

Optimized deterministic paths must not accidentally change behavior of the general matcher.

Run focused stress/semantic cases around:

- nested repetition;
- alternation explosions;
- captures within repetition;
- backreferences;
- zero-length loops;
- resource-limit exhaustion;
- cancellation during long searches; and
- malformed-input boundaries.

Ensure any optimized path participates correctly in match-state accounting where that accounting is part of the public resource-limit contract.

## 9. R2.5 — Consumer integration gate

Before declaring `Icod.CommandFramework 2.2.0` complete:

1. produce a prerelease package such as `2.2.0-alpha.N` through the repository's normal package lifecycle;
2. temporarily consume that prerelease from the open `Icod.Grep 1.6.0` PR;
3. run Grep's complete GNU grep 3.12 behavioral suite and package/archive validation;
4. run focused physical-reference Grep comparisons covering at least:
   - BRE ASCII sparse;
   - ERE ASCII dense;
   - BRE UTF-8 sparse;
   - BRE long-line;
   - BRE large-file; and
   - record-reader controls;
5. verify fixed-string and PCRE workloads have not regressed unexpectedly; and
6. only then finalize the stable `2.2.0` package contract.

## 10. Version and compatibility policy

`2.2.0` is appropriate because the release may add internal optimization structures and benchmark infrastructure while preserving source/binary behavioral compatibility for existing public consumers.

The production project carries:

- `<Version>2.2.0</Version>`
- `<PackageVersion>2.2.0</PackageVersion>`

with release notes describing the measurement-first managed regex performance work.

Prerelease package identifiers used for cross-repository validation do not change the eventual stable API contract.

## 11. CI policy

Ordinary PR CI continues to build and test CommandFramework on Windows, Linux, and macOS.

The benchmark project remains non-packable. CI runs deterministic smoke on all three host families and a real Windows BenchmarkDotNet exercise plus pinned-baseline collector orchestration. Hosted timings remain observational only.

Performance changes do not weaken Release warnings-as-errors or any existing package validation.

## 12. Definition of done

`Icod.CommandFramework 2.2.0` is complete when:

- the direct regex benchmark suite exists and has a retained `2.1.0` physical baseline;
- the principal managed BRE/ERE allocation hotspot has been materially reduced on measured workloads;
- improvements are attributable to specific engine changes rather than benchmark noise;
- all existing regex semantics and diagnostics remain green;
- cross-platform CommandFramework CI remains green;
- the prerelease package has passed `Icod.Grep 1.6.0` consumer integration;
- Grep's focused physical-reference measurements confirm the shared-engine improvement survives real command use; and
- documentation/release notes explain the performance work without promising unsupported universal percentages.

## 13. Execution order

The current execution order is:

1. **R2.0 — direct benchmark foundation / untouched 2.1.0 baseline** — complete
2. **R2.1 — conservative start-position / required-prefix acceleration** — complete; Candidate 3 accepted
3. **R2.2 — deterministic state-path allocation reduction** — complete; Candidate 3 accepted
4. **Remeasure complete direct CommandFramework workloads** — next
5. **R2.3 — decode/prepared-input investigation, if measurements still justify it**
6. **R2.4 — complex-pattern/resource-limit closure**
7. **R2.5 — prerelease package and Icod.Grep consumer validation**

As with Grep T6, measurement may reorder later tranches after the whole-suite remeasurement. The tranche labels remain stable for history even if execution priority changes.
