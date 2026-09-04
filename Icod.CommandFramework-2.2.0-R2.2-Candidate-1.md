# Icod.CommandFramework 2.2.0 — R2.2 Candidate 1

**Tranche:** R2.2 — deterministic state-path allocation reduction  
**Implementation head:** `122510596fcc2f4940d48cd9d2779a11e7b78513`  
**Reference baseline:** `2.1.0` at `460732c9f0cacb194bc6cd97c71612c492603eb6`  
**Status:** implemented; canonical CI and physical measurement pending

## Scope

Candidate 1 deliberately avoids a general AST/state-engine rewrite.

`LiteralPrefixCompiledRegularExpression` already proves when a compiled expression is a complete, capture-free plain literal. R2.1 used that proof only for unanchored search; `RequireMatchAtStart=true` still delegated to the general matcher, which decoded the input and then traversed the full `RegexMatchState` / `SequenceRegexNode` path.

Candidate 1 extends the already-proven complete-literal specialization to anchored matching.

For complete literals with the default unlimited `MaximumMatchStates`, anchored text and byte matching now:

1. decodes the authoritative input once;
2. validates the requested start boundary exactly;
3. compares the complete literal directly at that decoded position using the configured character-class provider;
4. rejects opaque malformed preserved-byte units as literal candidates;
5. constructs the public match directly from source boundaries; and
6. returns zero captures, which is valid because complete-literal eligibility requires `CaptureCount == 0`.

## Explicit non-goals

Candidate 1 does **not** accelerate:

- structured required-prefix expressions;
- captures or backreferences;
- alternation or repetition;
- classes or assertions;
- any expression with finite `MaximumMatchStates`.

Those cases continue through the existing matcher unchanged.

Finite resource limits never receive the wrapper because `LiteralPrefixCompiledRegularExpression.Create` returns the inner expression whenever `MaximumMatchStates != int.MaxValue`. Resource-limit state accounting is therefore unchanged by construction.

## Semantic coverage

The existing R2.1 tests already cover:

- anchored failure behavior;
- finite resource-limit fallback;
- syntax-profile equivalence;
- ignore-case character-provider semantics;
- malformed UTF-8 PreserveBytes behavior; and
- nonliteral fallback.

Candidate 1 adds focused tests for successful anchored complete literals at nonzero text and byte offsets.

## Quantitative gate

After canonical Staging CI is green, run the physical comparison with:

```text
*RegexDecodeBenchmarks*
```

This group is the correct first measurement because its `RequireMatchAtStart` scenarios bypass R2.1's unanchored-start search optimization and directly exercise the anchored complete-literal path.

The strongest expected signal should be the smaller anchored inputs, where state-machine allocation is a larger fraction of total cost. Large 256 KiB / 2 MiB cases remain useful controls but are expected to be dominated by decode/materialization allocation.

Acceptance requires:

- no semantic or CI regression;
- a repeatable allocation reduction on anchored complete-literal workloads larger than the R2.0 allocation noise floor; and
- no claim of elapsed-time improvement unless the physical timing delta is materially larger than the established host variance.

If the effect is too small to justify the extra path, revert Candidate 1 and proceed to direct `SequenceRegexNode` specialization instead of accumulating narrow shortcuts without measurable value.
