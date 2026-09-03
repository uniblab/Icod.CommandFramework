# Icod.CommandFramework 2.2.0 — Managed Regular-Expression Performance Roadmap

**Baseline:** `main` / `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Target release:** `2.2.0`  
**Consumer driver:** `Icod.Grep 1.6.0` T6 performance work  
**Theme:** reduce managed BRE/ERE search cost and allocation without changing regex semantics  
**Status:** R2.0 benchmark foundation implemented and cross-platform validated — physical `2.1.0` reference baseline pending

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

**Implementation status:** code-side complete and validated. Quantitative physical-reference baseline collection remains the R2.0 exit gate.

The implemented non-packable project is `benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj`. It uses only public CommandFramework regular-expression APIs, remains outside `Icod.CommandFramework.sln`, and does not change production regex source.

Implemented benchmark groups are:

- `RegexLiteralSearchBenchmarks` — unanchored BRE/ERE search by literal width, input length, and hit position;
- `RegexDecodeBenchmarks` — `RequireMatchAtStart` controls that isolate full-input decode/setup from repeated unanchored start scanning;
- `RegexStructuralBenchmarks` — alternation, `+`, `?`, bounded repetition, brackets/classes, word boundaries, captures/backreferences, anchors, and empty matches;
- `RegexRepeatedSearchBenchmarks` — repeated public `Match` calls over one record, modeling consumers such as grep `-o`; and
- `RegexCompileBenchmarks` — representative BRE/ERE compilation cost independent of matching.

The deterministic smoke covers representative hits, misses, one-character search, byte and valid UTF-8 inputs, malformed UTF-8 Preserve/Replace behavior, malformed UTF-8 Throw behavior, and all structural scenarios.

PR CI now builds and runs the existing 468-test CommandFramework suite plus the regex benchmark build/smoke on Windows, Linux, and macOS. Windows additionally runs the pinned-`2.1.0` comparison collector under Windows PowerShell 5.1 in `-Smoke` mode. Canonical workflow run `33696871818` passed on exact implementation head `df32f285aadf08ad7142b6dd07ae6961791e39fd`.

The collector `benchmarks/Collect-RegexReferenceComparison.ps1`:

1. pins the immutable `2.1.0` baseline commit `460732c9f0cacb194bc6cd97c71612c492603eb6`;
2. creates fresh sibling detached worktrees for both baseline and candidate;
3. overlays the current benchmark harness into the baseline worktree so both variants execute identical benchmark code;
4. restores/builds each variant once;
5. performs two alternating passes in ABBA order by default;
6. waits 30 seconds between variants by default;
7. records each pass independently with BenchmarkDotNet artifacts and reproducibility metadata;
8. records the sibling `Icod.Grep/hardware_inventory.txt` SHA-256 when available, without copying its contents; and
9. has a Windows-PowerShell-safe `-Smoke` mode exercised by PR CI.

### Required benchmark dimensions

#### Literal search

Measure BRE and ERE equivalents for:

- one-character literal;
- short literal (`TARGET`);
- longer literal sequence;
- hit at start;
- hit near middle;
- hit near end;
- complete miss; and
- repeated/dense hits where appropriate.

Use input lengths approximately:

- 80 bytes;
- 4 KiB;
- 256 KiB; and
- at least one multi-MiB stress case where practical.

#### Structural regex cases

Include representative:

- alternation;
- `*`, `+`, `?`, and bounded repetition as supported by syntax;
- dot and bracket classes;
- beginning/end assertions;
- word-boundary assertions;
- captures;
- backreferences; and
- expressions that can match empty input.

#### Encoding profiles

Measure:

- byte mode / Latin-1-preserving inputs;
- valid UTF-8; and
- malformed UTF-8 under each supported invalid-input policy where meaningful.

### R2.0 measurements

Capture at minimum:

- mean/median elapsed time;
- managed allocation per operation;
- GC counts where useful; and
- input length / scenario identity.

Add deterministic smoke cases to ordinary PR CI so the benchmark project remains portable on Windows, Linux, and macOS without turning hosted timings into pass/fail thresholds.

### R2.0 exit criterion

The code-side measurement foundation is complete. R2.0 closes quantitatively after the physical Windows reference host collects the untouched `2.1.0` versus current `2.2.0` infrastructure series and the results distinguish the relative contribution of decode materialization, unanchored start scanning, deterministic state transitions, and branching/capture machinery.

No production regex optimization begins before that physical-reference series is inspected.

## 5. R2.1 — Conservative start-position acceleration

The first likely implementation target is unanchored search.

Today, `Search` can invoke the full expression at every possible decoded unit until a match is found. For expressions with a provable first literal or mandatory literal prefix, most of those start attempts are impossible.

### Goal

Derive conservative search-start information from the already parsed AST and use it only when equivalence is provable.

Potential forms include:

- required first literal rune;
- fixed literal prefix;
- anchored-at-input / anchored-at-line metadata; or
- another immutable search-plan representation established at compile time.

### Correctness rules

Acceleration must fall back to the existing general search whenever certainty is unavailable.

Do **not** infer a mandatory prefix through constructs whose semantics make it optional or ambiguous without proof, including problematic alternation, optional repetition, backreferences, locale-sensitive constructs, or zero-length prefixes.

Examples:

- `TARGET` can safely require `T` and may safely require the complete literal prefix.
- `TARGET.*foo` can safely use the `TARGET` prefix.
- `T\(ARGET\)`, depending on syntax AST shape, may permit the same prefix if the capture itself does not make the text optional.
- `T\?ARGET`, `\(TARGET\|OTHER\)`, or expressions beginning with arbitrary classes should use only metadata that is mathematically valid for every match, otherwise fall back.

### Success criterion

Literal-hit/miss and long-record Grep BRE/ERE workloads should show a substantial reduction in complete expression start attempts and managed allocation, with zero behavioral change.

## 6. R2.2 — Deterministic state-path allocation reduction

After start-position acceleration, profile what remains.

Simple literal sequences currently traverse the same general machinery needed for branching regex graphs. That machinery allocates states and collection objects even when there is at most one possible successor.

Investigate safe specializations for deterministic AST paths, such as:

- literal nodes/sequences;
- simple assertions followed by deterministic nodes;
- deterministic bracket/class sequences; and
- sequence segments that cannot branch or capture.

Possible techniques include:

- immutable compile-time classification of deterministic nodes;
- direct position advancement without allocating intermediate `RegexMatchState` objects when captures cannot change;
- avoiding `List`/`HashSet` creation for a sequence stage known to have at most one candidate; and
- lazily introducing the general candidate-set machinery only when the AST actually branches.

Do not mutate reusable shared state in a way that compromises recursion, backtracking, captures, cancellation, resource counting, or thread safety.

## 7. R2.3 — Decode/prepared-input investigation

Each byte-oriented `Match` currently materializes a decoded `RegexInput`. Whether this remains a dominant cost after R2.1/R2.2 must be measured rather than assumed.

If it remains significant, evaluate options such as:

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

The production project now carries:

- `<Version>2.2.0</Version>`
- `<PackageVersion>2.2.0</PackageVersion>`

with release notes describing the measurement-first managed regex performance work. Production matching code remains behaviorally unchanged during R2.0.

Prerelease package identifiers used for cross-repository validation do not change the eventual stable API contract.

## 11. CI policy

Ordinary PR CI continues to build and test CommandFramework on Windows, Linux, and macOS.

R2.0 additionally builds the non-packable benchmark project and runs its deterministic smoke on all three host families. Windows also validates the pinned-baseline comparison orchestration under Windows PowerShell 5.1. BenchmarkDotNet quantitative output from hosted runners is observational only.

Performance changes do not weaken Release warnings-as-errors or any existing package validation.

## 12. Definition of done

`Icod.CommandFramework 2.2.0` is complete when:

- the direct regex benchmark suite exists and has a retained `2.1.0` baseline;
- the principal managed BRE/ERE allocation hotspot has been materially reduced on measured workloads;
- improvements are attributable to specific engine changes rather than benchmark noise;
- all existing regex semantics and diagnostics remain green;
- cross-platform CommandFramework CI remains green;
- the prerelease package has passed `Icod.Grep 1.6.0` consumer integration;
- Grep's focused physical-reference measurements confirm the shared-engine improvement survives real command use; and
- documentation/release notes explain the performance work without promising unsupported universal percentages.

## 13. Initial execution order

The planned implementation order is:

1. **R2.0 — direct benchmark foundation / untouched 2.1.0 baseline** — code-side complete; physical baseline pending
2. **R2.1 — conservative start-position acceleration**
3. **R2.2 — deterministic state-path allocation reduction**
4. **R2.3 — decode/prepared-input investigation, if measurements still justify it**
5. **R2.4 — complex-pattern/resource-limit closure**
6. **R2.5 — prerelease package and Icod.Grep consumer validation**

As with Grep T6, measurement may reorder later tranches after R2.0/R2.1. The tranche labels remain stable for history even if execution priority changes.
