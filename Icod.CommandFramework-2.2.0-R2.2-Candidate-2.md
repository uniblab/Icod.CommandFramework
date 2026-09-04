# Icod.CommandFramework 2.2.0 — R2.2 Candidate 2

**Tranche:** R2.2 — deterministic state-path allocation reduction  
**Implementation head:** `1d10f231de8519ce582d5189769dda78894a0380`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Status:** implemented; canonical CI and physical measurement pending

## Scope

Candidate 2 moves from the complete-literal wrapper into the general AST matcher.

`SequenceRegexNode` previously allocated a new `List<RegexMatchState>` and `HashSet<RegexMatchState>` for every sequence stage even when every child node could produce at most one successor and could not mutate captures.

Candidate 2 classifies a sequence at construction time as deterministic only when every child node is one of:

- `EmptyRegexNode`;
- `LiteralRegexNode`;
- `DotRegexNode`;
- `AssertionRegexNode`;
- `CharacterClassRegexNode`; or
- `BracketRegexNode`.

For those sequences, and only when `MaximumMatchStates == int.MaxValue`, matching now carries a single `RegexMatchState` through each existing child `Match` call instead of allocating per-stage candidate collections.

The child node implementations remain unchanged. Cancellation checks and child-level state registration therefore remain exactly where they were. The sequence still registers its final yielded state.

An internal invariant check throws if a node classified as deterministic ever returns more than one successor. This prevents future node changes from silently violating the optimization's proof.

## Explicit fallbacks

The existing collection-based sequence path remains in use whenever:

- a sequence contains a group;
- a sequence contains alternation;
- a sequence contains repetition;
- a sequence contains a backreference;
- any other node type is introduced without being explicitly proven single-successor and non-capturing; or
- `MaximumMatchStates` is finite.

This first sequence-level candidate therefore does not change finite resource-limit accounting.

## Quantitative gate

After canonical Staging CI is green, run:

```text
*RegexStructuralBenchmarks*
```

This suite contains both potential deterministic-sequence beneficiaries and explicit branching/capturing controls. Allocation remains the primary acceptance signal.

Candidate 2 should be retained only if deterministic structural workloads show a repeatable allocation reduction larger than the R2.0 noise floor while alternation/capture/repetition controls retain their expected semantics and do not show unexplained allocation changes.
