# Icod.CommandFramework 2.2.0 — Managed Regular-Expression Performance Roadmap

**Baseline:** `main` / `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Target release:** `2.2.0`  
**Consumer driver:** `Icod.Grep 1.6.0` T6 performance work  
**Theme:** reduce managed BRE/ERE search cost and allocation without changing regex semantics  
**Status:** R2.0 through R2.3 complete — R2.4 complex-pattern/resource-limit closure next

## 1. Why 2.2.0 exists

`Icod.Grep 1.6.0` T6.0 established a controlled physical-reference benchmark suite and identified the shared managed regular-expression implementation as the first-order allocation hotspot for BRE/ERE workloads.

On the reference Windows laptop, representative Grep measurements showed:

- roughly **604.59 MB** allocated by a short-record BRE workload versus **6.38 MB** for the comparable record-reader control;
- roughly **3,005.83 MB** allocated by a long-record BRE workload versus **53.54 MB** for the comparable record-reader control;
- roughly **4,835.96 MB** allocated by the large-file BRE workload; and
- fixed-string and PCRE workloads allocating only a few megabytes on their corresponding benchmark shapes.

The record-reader controls demonstrated that record materialization alone could not explain the managed allocation volume. Direct inspection and measurement isolated four distinct shared-engine costs:

- byte-input decode/materialization;
- repeated unanchored start attempts;
- object-heavy deterministic/general state propagation; and
- repeated preparation of the same record across successive public `Match` calls.

Version `2.2.0` addresses those costs in the shared regex provider so every Icod consumer benefits. `Icod.Grep` remains a downstream acceptance and performance consumer; it does not grow a private BRE/ERE implementation or semantic shortcut around CommandFramework.

## 2. Non-negotiable semantic contract

Performance work must preserve the complete public and tested regular-expression behavior of `2.1.0`, including:

- GNU Basic, GNU Extended, and GNU Emacs syntax behavior;
- leftmost-longest match selection;
- capture numbering, capture spans, repeated captures, and backreferences;
- bracket expressions, ranges, named classes, equivalence classes, and documented collating-element behavior;
- case-insensitive matching through the configured character-class provider;
- byte-preserving and string matching APIs;
- UTF-8 decoding modes and malformed-input policies;
- exact source-byte/UTF-16 coordinates and invalid split-boundary rejection;
- anchors, word assertions, empty matches, and zero-length behavior;
- cancellation;
- `MaximumMatchStates` resource-limit diagnostics;
- controlled diagnostics and error codes; and
- thread-safe reuse of compiled expressions and immutable prepared inputs.

No optimization is acceptable merely because it passes Grep's common cases. The complete CommandFramework regex suite remains the semantic authority.

## 3. Measurement discipline

The governing rule is:

> **Profile the shared engine directly, optimize the smallest proven bottleneck, then revalidate through real consumers.**

CommandFramework owns a non-packable BenchmarkDotNet project so decode cost, start-position cost, state-machine cost, capture/backreference cost, and repeated caller reuse can be measured independently of Grep command overhead.

Hosted CI timings are diagnostic only. Narrow quantitative claims come from controlled repeated measurements on the same physical Windows reference host used by Icod.Grep T6, with the reference hardware inventory SHA-256:

`d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`

## 4. R2.0 — Direct regex benchmark foundation

**Status:** complete.

The non-packable project `benchmarks/RegularExpressions.Benchmarks` provides:

- `RegexLiteralSearchBenchmarks`;
- `RegexDecodeBenchmarks`;
- `RegexStructuralBenchmarks`;
- `RegexRepeatedSearchBenchmarks`; and
- `RegexCompileBenchmarks`.

The pinned collector `benchmarks/Collect-RegexReferenceComparison.ps1` compares the immutable `2.1.0` baseline with the current candidate using fresh worktrees, identical benchmark code, two ABBA passes by default, cooldowns, independent artifact validation, and BenchmarkDotNet InProcess mode.

The retained report is `Icod.CommandFramework-2.2.0-R2.0-Reference-Baseline-Report.md`.

### R2.0 findings

- allocation noise across untouched baseline/candidate infrastructure is about **0.03%**;
- timing is materially noisier, with roughly **9.5%** median two-pass spread;
- large byte-mode anchored controls allocate about **18 bytes per input byte**, proving linear decode/materialization cost;
- a 256 KiB unanchored BRE literal miss allocates about **116.5 MiB** versus about **4.5 MiB** for its anchored control, roughly a **26×** allocation multiplier;
- literal width is not a first-order allocation driver;
- repeated public matching over one 64 KiB record allocates about **101 MiB**; and
- compile cost is negligible for the observed Grep problem.

R2.0 therefore ordered the initial optimization work as R2.1 start-position acceleration, R2.2 state-path reduction, post-R2.2 remeasurement, then R2.3 prepared-input work if still justified.

## 5. R2.1 — Conservative start-position acceleration

**Status:** complete; Candidate 3 accepted.

Candidate progression:

- Candidate 1 accelerated only complete plain literals;
- Candidate 2 removed duplicate successful-input decode from that specialization; and
- Candidate 3 added a conservative `RequiredLiteralPrefixAnalyzer` for structurally provable literal prefixes while preserving fallback whenever certainty is unavailable.

Retained reports:

- `Icod.CommandFramework-2.2.0-R2.1-Candidate-2.md`
- `Icod.CommandFramework-2.2.0-R2.1-Candidate-3.md`

Representative physical results versus `2.1.0`:

- 256 KiB literal end-hit/miss: **~116.5 MiB → ~4.5 MiB** allocation, about **96% lower**;
- 4 KiB middle hit: **~971 KiB → ~72 KiB**, about **92.5% lower**;
- 80-byte start hit: **~4.3 KiB → ~1.8 KiB**, about **58% lower**;
- `TAR(GE)?T`: about **85.2% lower allocation** and **75.8% lower mean time**; and
- `TAR(GE)+T`: about **85.1% lower allocation** and **78.5% lower mean time**.

Unsupported structural controls retain the ordinary matcher and unchanged measured allocation. Finite `MaximumMatchStates` bypasses start acceleration so state-accounting semantics remain compatible.

## 6. R2.2 — Deterministic state-path allocation reduction

**Status:** complete; Candidate 3 accepted.

Candidate progression:

- Candidate 1 extended complete-literal specialization to anchored matching;
- Candidate 2 introduced a deterministic `SequenceRegexNode` path but reproduced a **35–42%** timing regression on unchanged-allocation bounded repetition; and
- Candidate 3 split deterministic and general iterators behind a small non-iterator dispatcher, retaining the allocation benefit while eliminating the fallback regression.

Retained reports:

- `Icod.CommandFramework-2.2.0-R2.2-Candidate-1.md`
- `Icod.CommandFramework-2.2.0-R2.2-Candidate-2.md`
- `Icod.CommandFramework-2.2.0-R2.2-Candidate-3.md`

The deterministic path is limited to direct children proven single-successor and non-capturing:

- `EmptyRegexNode`;
- `LiteralRegexNode`;
- `DotRegexNode`;
- `AssertionRegexNode`;
- `CharacterClassRegexNode`; and
- `BracketRegexNode`.

Groups, alternation, repetition, backreferences, finite state limits, and future unproven node types retain the general collection path at the containing sequence level.

Representative physical results versus `2.1.0`:

- BRE/ERE alternation: **39.6% lower allocation**;
- BRE word-boundary: **51.3% lower allocation**;
- BRE capture/backreference: **25.3% lower allocation**;
- anchored ERE: **60.2% lower allocation**;
- ERE optional/repetition: about **85.2% lower allocation**.

Crucially, `bre-bounded-repetition` returned to the baseline allocation envelope and measured about **9.3% faster** than `2.1.0` after the Candidate 3 split.

## 7. Post-R2.2 whole-suite remeasurement

**Status:** complete.

The retained report is `Icod.CommandFramework-2.2.0-Post-R2.2-Whole-Suite-Report.md`.

The complete suite confirmed that R2.1/R2.2 retained their accepted gains while materially changing the residual cost profile:

- large anchored byte and UTF-8 workloads still paid essentially all linear decode/materialization allocation;
- repeated matching over one 64 KiB record improved from about **101.3 MiB** to about **73.3 MiB**, but repeated preparation remained disproportionately expensive; and
- literal, structural-prefix, and deterministic-sequence gains remained intact.

Decision: R2.3 became the highest-value remaining optimization target.

## 8. R2.3 — Immutable prepared-input reuse

**Status:** complete; Candidate 1 and Candidate 2 accepted.

### Candidate 1 — internal immutable reuse ceiling

Candidate 1 introduced an internal `PreparedRegexInput` and internal prepared-matching seam while leaving the existing public API untouched.

The design guarantees:

- byte preparation snapshots caller-owned storage once;
- decoded units and source-coordinate mappings are immutable;
- no mutable match context/state is stored on the prepared input;
- each match creates independent transient state;
- returned byte values cannot mutate the prepared snapshot; and
- one prepared input may be reused concurrently without locks.

Canonical semantic/CI validation was green in workflow **33884416156**.

Authoritative two-pass physical result over the 64 KiB repeated-search workload:

- ordinary repeated public `Match`: about **51.03 ms**, **73.216 MiB allocated**;
- internal immutable prepared reuse: about **0.252 ms**, **9.06 KiB allocated**;
- about **203× higher throughput**; and
- about **8,275× less managed allocation**.

Retained report: `Icod.CommandFramework-2.2.0-R2.3-Candidate-1.md`.

### Candidate 2 — package-ready public immutable byte surface

Inspection of frozen Icod.Grep PR #12 confirmed the same repeated-preparation shape in the real consumer: `RegularExpressionPattern.Find` repeatedly calls public `ICompiledRegularExpression.Match` over one record at changing start offsets, and `PatternSet.FindAll` repeats that path for `-o` and color-span enumeration.

Candidate 2 therefore exposes the smallest cross-assembly contract needed by Grep without changing `ICompiledRegularExpression`:

- `RegularExpressionPreparedByteInput`;
- `RegularExpressionPreparedByteInput.Prepare(...)`;
- `CompiledRegularExpressionPreparedInputExtensions.Match(...)`; and
- `CompiledRegularExpressionPreparedInputExtensions.MatchAsync(...)`.

The public type is byte-only. Text preparation remains internal until a real external consumer demonstrates need.

Native CommandFramework compiled expressions use the internal zero-redecode path. Third-party `ICompiledRegularExpression` implementations retain a compatibility fallback through their existing byte methods; the fallback receives its own source copy, so even a mutating external implementation cannot corrupt the prepared snapshot.

The prepared BenchmarkDotNet project was changed to use only the public Candidate 2 API and no longer has friend-assembly access.

Canonical workflow **33933148479** is green at physical-measurement head `4732442a6187de95817db610cbde8e028ca83b07`.

Authoritative two-pass public-only physical result:

| Metric | Ordinary public loop | Public prepared loop |
| --- | ---: | ---: |
| Mean time | **41.083 ms** | **0.212 ms** |
| Managed allocation | **73.216 MiB** | **9.06 KiB** |
| Improvement | — | **99.48% lower time / 99.9879% lower allocation** |

This corresponds to about **194× higher throughput** and **8,275× less managed allocation**. Prepared allocation was exactly **9.06 KiB in both passes**, reproducing Candidate 1 exactly.

Retained report: `Icod.CommandFramework-2.2.0-R2.3-Candidate-2.md`.

### R2.3 closure contract

The package contract is deliberately narrow:

- preparation is explicit;
- authoritative byte ownership is immutable;
- match state remains per-call and ephemeral;
- prepared inputs are safe for concurrent independent matches;
- `ICompiledRegularExpression` remains source/binary compatible;
- no mutable cursor, decoded storage, matcher state, cache, or pooling contract is public; and
- public text preparation remains out of scope.

## 9. R2.4 — Complex-pattern, cancellation, malformed-input, and resource-limit closure

**Status:** next.

R2.4 is a semantic/adversarial closure tranche, not a broad new optimization tranche. Its purpose is to prove that R2.1–R2.3 did not weaken the general matcher or the prepared-input contract under difficult inputs.

Focused coverage must include:

- nested repetition and alternation explosions;
- captures within repetition and repeated capture-register behavior;
- backreferences;
- zero-length and empty-match progression;
- finite `MaximumMatchStates` exhaustion through both ordinary and prepared paths;
- cancellation during decode/preparation and long searches;
- malformed UTF-8 under `PreserveBytes`, `Replace`, and `Throw`;
- exact UTF-8 source-boundary rejection;
- concurrent prepared-input matching; and
- fallback behavior where optimized eligibility is deliberately unavailable.

R2.4 should prefer tests and focused benchmark/control cases over further architectural change. Any newly discovered semantic regression must be fixed before consumer packaging.

## 10. R2.5 — Prerelease package and Icod.Grep consumer validation

Before declaring `Icod.CommandFramework 2.2.0` complete:

1. produce a prerelease package such as `2.2.0-alpha.N` through the repository's normal package lifecycle;
2. consume that prerelease from open Icod.Grep PR #12;
3. integrate `RegularExpressionPreparedByteInput` into Grep's managed BRE/ERE repeated-record path;
4. run Grep's complete GNU grep 3.12 behavioral suite and package/archive validation;
5. run focused physical-reference Grep comparisons covering at least:
   - BRE ASCII sparse;
   - ERE ASCII dense;
   - BRE UTF-8 sparse;
   - BRE long-line;
   - BRE large-file; and
   - record-reader controls;
6. verify fixed-string and PCRE workloads have not regressed unexpectedly; and
7. only then finalize the stable `2.2.0` package contract.

Icod.Grep PR #12 remains frozen at its T6.0 baseline until this prerelease consumer gate begins; it should not be made intentionally unbuildable against an unpublished API.

## 11. Version and compatibility policy

`2.2.0` remains appropriate. Existing `ICompiledRegularExpression` consumers retain their source/binary contract, while the release adds optional public immutable prepared-byte reuse.

The production project carries:

- `<Version>2.2.0</Version>`
- `<PackageVersion>2.2.0</PackageVersion>`

Prerelease identifiers used for cross-repository validation do not change the eventual stable API contract.

## 12. CI policy

Ordinary PR CI builds and tests CommandFramework on Windows, Linux, and macOS.

Benchmark projects remain non-packable. CI runs deterministic smokes on all supported host families and Windows BenchmarkDotNet/collector exercises. Hosted timings remain observational only.

Release warnings-as-errors and existing package validation remain unchanged.

PR #9 remains Draft throughout R2 development.

## 13. Definition of done

`Icod.CommandFramework 2.2.0` is complete when:

- the direct regex benchmark suite and retained `2.1.0` physical baseline exist;
- R2.1, R2.2, and R2.3 accepted gains remain documented and reproducible;
- ordinary and prepared matching preserve the semantic/diagnostic contract;
- R2.4 adversarial/resource-limit/cancellation closure is green;
- cross-platform CommandFramework CI remains green;
- a prerelease package passes Icod.Grep 1.6.0 consumer integration;
- Grep's focused physical-reference measurements confirm the shared-engine improvement survives real command use; and
- release documentation explains measured improvements without promising unsupported universal percentages.

## 14. Current execution order

1. **R2.0 — direct benchmark foundation / untouched 2.1.0 baseline** — complete
2. **R2.1 — conservative start-position / required-prefix acceleration** — complete; Candidate 3 accepted
3. **R2.2 — deterministic state-path allocation reduction** — complete; Candidate 3 accepted
4. **Post-R2.2 whole-suite remeasurement** — complete
5. **R2.3 — immutable prepared-input reuse** — complete; Candidates 1 and 2 accepted
6. **R2.4 — complex-pattern / cancellation / malformed-input / resource-limit closure** — next
7. **R2.5 — prerelease package and Icod.Grep consumer validation**

The tranche labels remain stable for history even when measurement changes execution priority.