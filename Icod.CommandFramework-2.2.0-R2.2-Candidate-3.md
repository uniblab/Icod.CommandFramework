# Icod.CommandFramework 2.2.0 — R2.2 Candidate 3

**Tranche:** R2.2 — deterministic state-path allocation reduction  
**Candidate 2 implementation head:** `1d10f231de8519ce582d5189769dda78894a0380`  
**Candidate 2 repaired measurement head:** `a49e1b82de555218dcbf687a267c1ab11537441c`  
**Candidate 3 implementation head:** `fafcd39f94213a1e868d40bbbb7f0c08026203ba`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Status:** implemented; canonical CI and physical validation pending

## Why Candidate 3 exists

Candidate 2 proved that deterministic `SequenceRegexNode` paths can remove substantial allocation from the shared matcher. It also revealed a repeatable timing regression on an unchanged-allocation fallback workload.

Two independent two-pass ABBA structural comparisons reproduced the issue for `bre-bounded-repetition` (`TA\\{1,2\\}RGET`). Allocation remained exactly `971.81 KiB` in every baseline and candidate leg, but Candidate 2 was materially slower in both collections:

- first collection: approximately 41.6% slower by two-pass mean;
- independent confirmation: approximately 35.3% slower by two-pass mean.

Because the workload is not eligible for deterministic sequence acceleration and its allocation is byte-for-byte unchanged, the leading hypothesis is code-generation/state-machine interference from placing both deterministic and general iterator logic in the same `SequenceRegexNode.Match` iterator.

Candidate 2 therefore remains conceptually accepted but is not accepted as the final R2.2 implementation.

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

This separation ensures an ineligible sequence no longer instantiates or executes the deterministic iterator state machine.

## Eligibility remains unchanged

A sequence is deterministic only when every direct child is one of:

- `EmptyRegexNode`;
- `LiteralRegexNode`;
- `DotRegexNode`;
- `AssertionRegexNode`;
- `CharacterClassRegexNode`; or
- `BracketRegexNode`.

The deterministic path is also disabled whenever `MaximumMatchStates` is finite.

Groups, alternation, repetition, backreferences, and any future unproven node type therefore continue through `MatchGeneral` at the containing sequence level. Nested deterministic child sequences may still optimize independently, which explains Candidate 2's measured allocation reductions inside several structurally complex benchmarks.

## Candidate 2 performance evidence retained

Candidate 2's deterministic allocation gains were large and reproduced independently. Representative allocation reductions versus 2.1.0 included approximately:

- 29.4% for BRE and ERE alternation;
- 38.1% for BRE word-boundary;
- 58.6% for anchored ERE;
- 18.8% for BRE capture/backreference; and
- 85.2% for the R2.1 optional/repetition required-prefix workloads.

`bre-bounded-repetition`, `bre-bracket-class`, and `bre-empty-match` retained baseline allocation, confirming their outer fallback behavior.

Candidate 3 must preserve the useful allocation reductions while removing the bounded-repetition timing regression.

## Acceptance gate

After canonical Staging CI is green, run the authoritative physical comparison:

```powershell
powershell .\\benchmarks\\Collect-RegexReferenceComparison.ps1 `
    -Filter '*RegexStructuralBenchmarks*' `
    -Passes 2 `
    -CooldownSeconds 30
```

Acceptance requires:

1. the deterministic allocation reductions remain materially larger than the R2.0 allocation noise floor;
2. `bre-bounded-repetition` returns to the baseline timing envelope rather than reproducing the Candidate 2 ~35–42% slowdown;
3. `bre-capture-backreference` is rechecked because Candidate 2 reduced its allocation but showed a smaller timing concern in one confirmation series;
4. unchanged-allocation controls remain stable;
5. finite `MaximumMatchStates`, cancellation, captures, alternation, repetition, assertions, malformed-input, and syntax-profile semantics remain green; and
6. no new cross-platform CI regression is introduced.

If those conditions hold, Candidate 3 should replace Candidate 2 as the retained R2.2 implementation and R2.2 can then be evaluated for closure or one final narrowly targeted refinement.
