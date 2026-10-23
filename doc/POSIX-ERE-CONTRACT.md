# Icod.Expressions: draft strict POSIX ERE contract

**Status:** Standards-facing design draft for A01; no strict ERE provider exists
yet. Authority: [POSIX.1-2024 Issue 8, Base Definitions chapter 9,
especially §§9.1–9.2 and 9.4](https://pubs.opengroup.org/onlinepubs/9799919799/basedefs/V1_chap09.html).
The inherited GNU engine is pinned to CommandFramework
`e1e118f18246eed290bee4758325a20e302bfb92`. This draft does not
certify `GnuExtendedRegularExpressionProvider` as strict POSIX.

## Proposed boundary

`Icod.Expressions.Ere` will define a separately selectable standards-facing
ERE grammar and leftmost-longest matching contract. Gnulib
`RE_SYNTAX_POSIX_EXTENDED` belongs in `Icod.Expressions.Gnu`: the macro is a
named GNU bit composition, not an alias for the strict public ERE surface.
Encoding, coordinates, cancellation, and resource controls remain explicit
managed policies around the language contract.

| Topic | Issue 8 reference and strict target | Inherited engine / required evidence |
|---|---|---|
| Operator spelling | §9.4 defines unescaped grouping, alternation, repetition and interval forms, ordinary versus special characters, and anchoring. | Extended parses those forms. A 15-case independent glibc differential identifies a **defined §9.4.9 divergence**: inherited Extended matches `a^b`/`a$b` literally; a strict ERE must treat the middle anchors as assertions and therefore find no match on those literal inputs. Escaped `a\^b`/`a\$b` agree as literals. Other malformed forms still need case-by-case classification. |
| Backreferences | §9.4 is the ERE language definition; BRE backreference rules are in §9.3. | The current Extended parser accepts `\\1`–`\\9` as a GNU extension, verified by `CapturesAndBackReferencesUseExtendedGrouping`. Keep the inherited behavior in the GNU corpus; do not promise it as portable ERE. |
| Brackets and locale | §9.4's bracket rules rely on the shared bracket requirements of §9.3.5. | Single-scalar classes/ranges are tested, but multi-scalar collating elements and locale-wide equivalence are not implemented. Record the limitation before any conformance claim. |
| Whole match and subpatterns | §9.1 defines leftmost-longest selection and subpattern choices. | Local tests establish some longest matches and capture ties, but no external Issue 8 differential corpus yet. Compare whole and subgroup spans, not just success/failure. |
| Newline, dot, and subject | §9.2 and each utility/interface specify newline processing. | Current Extended options include `NewLineSensitive`, `LineSeparator`, and `DotMatchesNull`; byte and UTF-8 decoding options are managed policies. Avoid treating `grep -z` or Sed `M` as intrinsic ERE syntax. |
| Failure and resource policy | §§9.2 and 9.4 distinguish invalid patterns from cases whose result is undefined/unspecified. | Current diagnostics and `MaximumNestingDepth`/`MaximumMatchStates` are controlled outcomes. Classify standards errors separately from managed resource failures and GNU-defined acceptance. |

## Differential fixture rule

For each pattern, record input, locale/provider, encoding and coordinates,
start offset, all capture spans, Issue 8 section, and classification as
**standard-required**, **standard-invalid**, **undefined/unspecified**,
**GNU-defined**, or **managed policy**. A result shared by a GNU and a strict
surface is not proof the two contracts are aliases. The pinned Gnulib
configuration and strict standard are separate oracle columns.

## Open acceptance work

1. Build a pinned standards oracle corpus for defined ERE grammar, anchors,
   brackets and captures, including locales with relevant collation behavior.
   A first [Linux/glibc C-locale comparison](Icod.Expressions-A04-POSIX-Glibc-Differential.md)
   now covers four original defined ASCII ERE cases, two defined
   middle-anchor divergences, two escaped-anchor controls, and two
   anchor-branch capture observations. The [included Gnulib runtime check](Icod.Expressions-A04-Gnulib-Capture-Comparison.md)
   agrees for those anchor cases; broader capture and locale rules remain open.
2. Characterize GNU-only ERE backreferences, malformed intervals, and operator
   contexts as inherited divergence cases before normalization.
3. Resolve multi-scalar collation and capture tie behavior, or state their
   precise limitations; neither is proven by the present local test suite.
4. Keep CommandFramework's source and consumer-visible behavior intact during
   audit, extraction, and parity validation.

See the [A01 audit](Icod.Expressions-A01-POSIX-Semantic-Audit.md),
[A02 GNU matrix](Icod.Expressions-A02-GNU-Syntax-Bit-Matrix.md), and
[A04 characterization index](Icod.Expressions-A04-Characterization-Index.md).
