# Icod.CommandFramework 2.2.0 — R2.2 Candidate 3

**Tranche:** R2.2 — deterministic state-path allocation reduction  
**Candidate 2 implementation head:** `1d10f231de8519ce582d5189769dda78894a0380`  
**Candidate 2 repaired measurement head:** `a49e1b82de555218dcbf687a267c1ab11537441c`  
**Candidate 3 implementation head:** `fafcd39f94213a1e868d40bbbb7f0c08026203ba`  
**Candidate 3 measurement head:** `af526793507d7bab307a3ed8cc87e8efbb33a4da`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Canonical Staging validation:** workflow run `33859421589` — green  
**Status:** physically validated and accepted; R2.2 complete

## Why Candidate 3 exists

Candidate 2 proved that deterministic `SequenceRegexNode` paths can remove substantial allocation from the shared matcher. It also revealed a repeatable timing regression on an unchanged-allocation fallback workload.

Two independent two-pass ABBA structural comparisons reproduced the issue for `bre-bounded-repetition` (`TA\\{1,2\\}RGET`). Allocation remained exactly `971.81 KiB` in every baseline and candidate leg, but Candidate 2 was materially slower in both collections:

- first collection: approximately 41.6% slower by two-pass mean;
- independent confirmation: approximately 35.3% slower by two-pass mean.

The second Candidate 2 confirmation also showed `bre-capture-backreference` about 14.4% slower despite an 18.8% allocation reduction.

Because bounded repetition is not eligible for deterministic sequence acceleration and its allocation was byte-for-byte unchanged, the leading hypothesis was code-generation/state-machine interference from placing both deterministic and general iterator logic in the same `SequenceRegexNode.Match` iterator.

Candidate 2 therefore remains conceptually important but is superseded by Candidate 3 as the retained R2.2 implementation.

## Candidate 3 refinement

Candidate 3 preserves Candidate 2's eligibility proof and state semantics while isolating iterator implementations.

`SequenceRegexNode.Match` is now a small non-iterator dispatcher:

```text
Match
  deterministic + unlimited MaximumMatchStates -> MatchDeterministic
  otherwise                                     -> MatchGeneral
```

`MatchDeterministic` contains the Candidate 2 single-state path.

`MatchGeneral` contains the pre-optimization collection path as nearly verbatim as practical:

1. seed a `List<RegexMatchState>` with the input state;
2. for each child node, build the next `List<RegexMatchState>`;
3. deduplicate each stage with `HashSet<RegexMatchState>`;
4. terminate when a stage produces no states; and
5. register/yield the final states.

This separation prevents an ineligible sequence from sharing the deterministic iterator state machine. It also removes the former outer `Match` iterator allocation entirely because `Match` now returns the selected child iterator directly.

## Eligibility remains unchanged

A sequence is deterministic only when every direct child is one of:

- `EmptyRegexNode`;
- `LiteralRegexNode`;
- `DotRegexNode`;
- `AssertionRegexNode`;
- `CharacterClassRegexNode`; or
- `BracketRegexNode`.

The deterministic path is also disabled whenever `MaximumMatchStates` is finite.

Groups, alternation, repetition, backreferences, and any future unproven node type therefore continue through `MatchGeneral` at the containing sequence level. Nested deterministic child sequences may still optimize independently.

## Physical acceptance collection

The authoritative Candidate 3 comparison used:

- baseline `460732c9f0cacb194bc6cd97c71612c492603eb6`;
- candidate `af526793507d7bab307a3ed8cc87e8efbb33a4da`;
- BenchmarkDotNet `InProcess` mode;
- filter `*RegexStructuralBenchmarks*`;
- two passes in ABBA order;
- 30-second cooldowns;
- the same physical Windows reference laptop;
- hardware inventory SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`;
- BenchmarkDotNet `0.15.8` on .NET `10.0.11`; and
- all 10 structural workloads in every pass.

Two-pass means were:

| Scenario | 2.1.0 mean | Candidate 3 mean | Time change | 2.1.0 allocated | Candidate 3 allocated | Allocation change |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `bre-alternation` | 692.45 µs | 464.38 µs | -32.9% | 2508.53 KiB | 1514.47 KiB | -39.6% |
| `bre-bounded-repetition` | 308.07 µs | 279.41 µs | -9.3% | 971.81 KiB | 971.81 KiB | 0.0% |
| `bre-bracket-class` | 475.27 µs | 449.87 µs | -5.3% | 1258.51 KiB | 1258.51 KiB | 0.0% |
| `bre-capture-backreference` | 639.46 µs | 492.36 µs | -23.0% | 1963.23 KiB | 1466.45 KiB | -25.3% |
| `bre-empty-match` | 0.911 µs | 0.850 µs | -6.8% | 2.57 KiB | 2.57 KiB | 0.0% |
| `bre-word-boundary` | 315.87 µs | 195.61 µs | -38.1% | 972.45 KiB | 473.60 KiB | -51.3% |
| `ere-alternation` | 754.51 µs | 452.38 µs | -40.0% | 2508.54 KiB | 1514.47 KiB | -39.6% |
| `ere-anchor` | 1.362 µs | 0.712 µs | -47.7% | 3.67 KiB | 1.46 KiB | -60.2% |
| `ere-optional` | 315.31 µs | 57.07 µs | -81.9% | 1004.75 KiB | 148.22 KiB | -85.2% |
| `ere-repetition` | 283.08 µs | 57.05 µs | -79.8% | 1005.22 KiB | 148.45 KiB | -85.2% |

## Candidate 2 regression closure

The primary acceptance question was `bre-bounded-repetition`.

Candidate 2 had reproduced a roughly 35–42% slowdown across two independent collections while allocation remained exactly `971.81 KiB`. Candidate 3 instead measures **9.3% faster than 2.1.0**, with the same exact `971.81 KiB` allocation. The regression is therefore removed.

The secondary concern, `bre-capture-backreference`, also closes cleanly. Candidate 2's confirmation had been about 14.4% slower; Candidate 3 is **23.0% faster than 2.1.0** while allocation falls **25.3%**.

The other unchanged-allocation controls remain stable: bounded repetition, bracket class, and empty match retain exactly the baseline allocation. Their elapsed-time movements are all favorable and within a range that does not suggest a regression.

## Additional improvement from iterator isolation

Candidate 3 does more than restore fallback timing. Because `SequenceRegexNode.Match` is no longer itself an iterator, it also removes an outer iterator allocation around both selected paths.

Compared with Candidate 2's independent confirmation, Candidate 3 further reduces measured allocation for several nested deterministic cases:

- BRE/ERE alternation: about `1770.66 KiB -> 1514.47 KiB`;
- BRE word-boundary: about `601.70 KiB -> 473.60 KiB`;
- BRE capture/backreference: about `1594.54 KiB -> 1466.45 KiB`;
- anchored ERE: about `1.52 KiB -> 1.46 KiB`.

The R2.1 optional/repetition workloads retain their approximately 85.2% allocation reductions.

## R2.2 decision

Candidate 3 is accepted and replaces Candidate 2 as the retained deterministic-sequence implementation.

R2.2 is complete because:

1. deterministic sequence stages no longer allocate per-stage `List<RegexMatchState>` / `HashSet<RegexMatchState>` collections;
2. the optimization remains restricted to provably single-successor, non-capturing direct child nodes;
3. finite `MaximumMatchStates` retains the general matcher and therefore the existing resource-accounting contract;
4. nested deterministic sequences can benefit inside otherwise complex structures without changing the outer branching/capturing semantics;
5. Candidate 2's reproducible fallback timing regression is eliminated;
6. the physical allocation improvements are vastly larger than the R2.0 noise floor; and
7. canonical Windows/Linux/macOS PR CI is green at workflow run `33859421589`.

The next step is to **remeasure the direct CommandFramework benchmark suite with R2.1 and R2.2 both in place** before deciding whether R2.3 decode/prepared-input work remains the highest-value next target.
