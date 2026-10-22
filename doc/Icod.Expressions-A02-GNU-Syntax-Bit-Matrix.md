# Icod.Expressions A02: GNU syntax-bit and predefined-profile matrix

**Status:** In-situ audit draft; no production behavior changes. The frozen upstream
source is [`coreutils/gnulib` `fb7312fa8d3df29f0ca0678f669b9a5b88a078ec`,
`lib/regex.h` lines 67–283](https://github.com/coreutils/gnulib/blob/fb7312fa8d3df29f0ca0678f669b9a5b88a078ec/lib/regex.h#L67-L283).
The current managed engine is
[`Icod.CommandFramework` `e1e118f18246eed290bee4758325a20e302bfb92`](https://github.com/uniblab/Icod.CommandFramework/tree/e1e118f18246eed290bee4758325a20e302bfb92/src/RegularExpressions).
This is a semantic mapping, not a promise to expose Gnulib's C bitmask or ABI.

`Implemented` means the existing API models the control; `Partial` means only
fixed or coupled behavior is present; `Missing-Required` means the roadmap's
GNU language target needs a control; `Designed-Defer` requires a documented
future decision; `Not-Applicable/Internal` and `Out-of-Scope-ABI` are excluded
from the public language contract. An observed match is not a substitute for
an independently selectable option. Test references below indicate existing
coverage, not a complete differential characterization corpus.

## One-to-one syntax-bit disposition

| Pinned bit | Managed meaning; affected stage | Current implementation / tests | Known consumer; proposed managed concept | Disposition |
|---|---|---|---|---|
| `RE_BACKSLASH_ESCAPE_IN_LISTS` | Backslash quotes the next bracket character; parser. | Emacs takes backslash literally in lists; no independent toggle. `GnuEmacsRegularExpressionTests`. | Gnulib Awk profiles; `BackslashEscapeInBracket`. | Missing-Required |
| `RE_BK_PLUS_QM` | Escaped `+`/`?` instead of bare forms; parser. | Basic escaped, Extended/Emacs bare; coupled to `Syntax`. Basic/Extended/Emacs tests. | grep BRE/ERE, ed/sed; `BackslashPlusQuestionMark`. | Partial |
| `RE_CHAR_CLASSES` | Enable POSIX bracket classes; parser/provider. | Always parsed; Basic bracket tests. | grep, sed, ed; `RecognizeCharacterClasses`. | Partial |
| `RE_CONTEXT_INDEP_ANCHORS` | `^`/`$` anchors regardless of branch position; parser. | Anchors only at branch boundaries; no toggle. Basic/Extended anchor tests. | Extended Gnulib profiles; `ContextIndependentAnchors`. | Missing-Required |
| `RE_CONTEXT_INDEP_OPS` | Repetition special even in otherwise literal contexts; parser. | Leading repetition is diagnosed or treated by coupled permissive switch; no independent policy. Repetition tests. | Extended profiles; `ContextIndependentOperators`. | Partial |
| `RE_CONTEXT_INVALID_OPS` | Reject leading `*`, `+`, `?`, `{`; parser/diagnostic. | Partly governed by `AllowInvalidRepetitionOperators`, also coupled with adjacent-duplicate handling. Repetition tests. | Extended profiles; `RejectLeadingOperators`. | Partial |
| `RE_DOT_NEWLINE` | Dot matches newline; matcher. | `NewLineSensitive`/`LineSeparator` control the chosen separator, with Emacs special treatment; independent Gnulib bit absent. Line-sensitive tests. | grep/sed; `DotMatchesNewline`. | Partial |
| `RE_DOT_NOT_NULL` | Dot excludes NUL; matcher. | `DotMatchesNull` inverts it, with profile-specific behavior; NUL tests. | grep `-z`, sed `-z`; `DotMatchesNull`. | Implemented |
| `RE_HAT_LISTS_NOT_NEWLINE` | Negated bracket excludes newline; matcher. | Coupled to `NewLineSensitive` rather than independent of dot; bracket tests. | grep/sed; `NegatedBracketExcludesNewline`. | Partial |
| `RE_INTERVALS` | Recognize interval repetition; parser. | Always enabled for Basic/Extended/Emacs; interval tests. | All current regex consumers; `RecognizeIntervals`. | Partial |
| `RE_LIMITED_OPS` | Disable `+`, `?`, `|` operators; parser. | No independent toggle. Basic and Extended always recognize these operators. | Gnulib minimal Basic; `LimitedOperators`. | Missing-Required |
| `RE_NEWLINE_ALT` | Pattern newline is alternation; parser. | No independent toggle; parser alternation is `\\|` or `|`. | Gnulib Grep/Egrep profiles; `NewlineIsAlternation`. | Missing-Required |
| `RE_NO_BK_BRACES` | Bare rather than escaped interval braces; parser. | Extended bare, Basic/Emacs escaped, coupled to `Syntax`; interval tests. | grep ERE, sed `-E`; `BareIntervalBraces`. | Partial |
| `RE_NO_BK_PARENS` | Bare rather than escaped group parentheses; parser. | Extended bare, Basic/Emacs escaped; grouping tests. | grep ERE, ed/sed; `BareGroupParentheses`. | Partial |
| `RE_NO_BK_REFS` | Disable numeric backreferences; parser. | `\\1`–`\\9` recognized in all three modes; no toggle. Basic/Extended backreference tests. | Gnulib Awk/minimal Extended; `RecognizeBackreferences` (inverse). | Missing-Required |
| `RE_NO_BK_VBAR` | Bare rather than escaped alternation bar; parser. | Extended bare, Basic/Emacs escaped; alternation tests. | grep ERE, sed `-E`; `BareAlternationBar`. | Partial |
| `RE_NO_EMPTY_RANGES` | Reject reversed ranges rather than treat as empty; bracket parser. | Inverse of `AllowEmptyRanges`; Basic/profile bracket tests. | GNU expr preset, grep; `AllowReverseRangeAsEmpty`. | Implemented |
| `RE_UNMATCHED_RIGHT_PAREN_ORD` | Unmatched `)` is literal rather than error; parser. | Extended unmatched `)` is literal, Basic escaped unmatched `\\)` is diagnosed; behavior is coupled to `Syntax`. `UnmatchedExtendedClosingParenthesisIsLiteral`. | Gnulib Extended profiles; `UnmatchedRightParenIsLiteral`. | Partial |
| `RE_NO_POSIX_BACKTRACKING` | Succeed at first whole-pattern match; matcher selection. | Current matcher uses leftmost-longest; no policy switch. Longest-match tests. | No confirmed current need; `MatchSelectionPolicy` (first success vs longest). | Designed-Defer |
| `RE_NO_GNU_OPS` | Disable GNU word/boundary/subject operators; parser. | GNU escapes are recognized, without a global disable switch. Basic/Extended GNU-operator tests. | Awk/PosixAwk, strict contracts; `RecognizeGnuOperators` (inverse). | Missing-Required |
| `RE_DEBUG` | Native `regex.c` conditional debugging; implementation. | No corresponding managed syntax contract. | None; no public option. | Not-Applicable/Internal |
| `RE_INVALID_INTERVAL_ORD` | Malformed interval becomes ordinary text; parser. | Extended has some literal fallback while other malformed forms diagnose; no independent switch. Interval diagnostics tests. | Gnulib Egrep/Awk; `InvalidIntervalIsOrdinary`. | Partial |
| `RE_ICASE` | Ignore case; comparison/provider and matcher. | `IgnoreCase`; case-insensitive tests. | grep `-i`, ptx; `IgnoreCase`. | Implemented |
| `RE_CARET_ANCHORS_HERE` | Native parser helper for caret; implementation. | Not a semantic user option. | None; no public option. | Not-Applicable/Internal |
| `RE_CONTEXT_INVALID_DUP` | Invalid leading/adjacent duplicate operator; parser/diagnostic. | `AllowInvalidRepetitionOperators` inverts a subset but also changes other contexts. Basic/Extended repetition tests. | GNU expr preset; `RejectInvalidDuplicate`. | Partial |
| `RE_NO_SUB` | Suppress submatch result production; compile/result. | Captures always materialized; no result policy. Capture tests. | Consumers needing existence only; `CaptureResultPolicy`. | Missing-Required |

No syntax bit above is classed `Out-of-Scope-ABI`: that disposition applies to
the surrounding GNU/BSD C structs, global `re_syntax_options`, allocators,
fastmap pointers, and calling conventions. The managed profiles should store
immutable configuration values instead of process-wide mutable syntax state.

## Exact named compositions at the pinned revision

The expressions below transcribe the pinned header; `COMMON` means its
`_RE_SYNTAX_POSIX_COMMON`, and `PB`/`PE` are the two corresponding named
Gnulib expressions. These names identify *Gnulib syntax settings*, not strict
POSIX contracts or complete application emulations.

| Header name | Exact expression, with abbreviations expanded immediately below | Current managed approximation / target |
|---|---|---|
| `RE_SYNTAX_EMACS` | `RE_CHAR_CLASSES | RE_INTERVALS` | `GnuEmacsCompatibility` approximates; exact bit parity unproved. |
| `RE_SYNTAX_AWK` | `RE_BACKSLASH_ESCAPE_IN_LISTS | RE_DOT_NOT_NULL | RE_NO_BK_PARENS | RE_NO_BK_REFS | RE_NO_BK_VBAR | RE_NO_EMPTY_RANGES | RE_DOT_NEWLINE | RE_CONTEXT_INDEP_ANCHORS | RE_CHAR_CLASSES | RE_UNMATCHED_RIGHT_PAREN_ORD | RE_NO_GNU_OPS` | Missing named profile. |
| `RE_SYNTAX_GNU_AWK` | `(PE | RE_BACKSLASH_ESCAPE_IN_LISTS | RE_INVALID_INTERVAL_ORD) & ~(RE_DOT_NOT_NULL | RE_CONTEXT_INDEP_OPS | RE_CONTEXT_INVALID_OPS)` | Missing named profile. |
| `RE_SYNTAX_POSIX_AWK` | `PE | RE_BACKSLASH_ESCAPE_IN_LISTS | RE_INTERVALS | RE_NO_GNU_OPS | RE_INVALID_INTERVAL_ORD` | Missing named profile. |
| `RE_SYNTAX_GREP` | `(PB | RE_NEWLINE_ALT) & ~(RE_CONTEXT_INVALID_DUP | RE_DOT_NOT_NULL)` | Missing exact named profile. |
| `RE_SYNTAX_EGREP` | `(PE | RE_INVALID_INTERVAL_ORD | RE_NEWLINE_ALT) & ~(RE_CONTEXT_INVALID_OPS | RE_DOT_NOT_NULL)` | Missing exact named profile. |
| `RE_SYNTAX_POSIX_EGREP` | `RE_SYNTAX_EGREP` | Alias in header; missing managed alias. |
| `RE_SYNTAX_ED` | `RE_SYNTAX_POSIX_BASIC` | Alias in header; missing managed alias. |
| `RE_SYNTAX_SED` | `RE_SYNTAX_POSIX_BASIC` | Alias in header; missing managed alias. |
| `RE_SYNTAX_POSIX_BASIC` (`PB`) | `COMMON | RE_BK_PLUS_QM | RE_CONTEXT_INVALID_DUP` | Current `Basic` is not an exact profile or strict POSIX contract. |
| `RE_SYNTAX_POSIX_MINIMAL_BASIC` | `COMMON | RE_LIMITED_OPS` | Missing named profile. |
| `RE_SYNTAX_POSIX_EXTENDED` (`PE`) | `COMMON | RE_CONTEXT_INDEP_ANCHORS | RE_CONTEXT_INDEP_OPS | RE_NO_BK_BRACES | RE_NO_BK_PARENS | RE_NO_BK_VBAR | RE_CONTEXT_INVALID_OPS | RE_UNMATCHED_RIGHT_PAREN_ORD` | Current `Extended` is not an exact profile or strict POSIX contract. |
| `RE_SYNTAX_POSIX_MINIMAL_EXTENDED` | `COMMON | RE_CONTEXT_INDEP_ANCHORS | RE_CONTEXT_INVALID_OPS | RE_NO_BK_BRACES | RE_NO_BK_PARENS | RE_NO_BK_REFS | RE_NO_BK_VBAR | RE_UNMATCHED_RIGHT_PAREN_ORD` | Missing named profile. |

`COMMON = RE_CHAR_CLASSES | RE_DOT_NEWLINE | RE_DOT_NOT_NULL |
RE_INTERVALS | RE_NO_EMPTY_RANGES` (the upstream private expression, not an
extra public profile). There are **13 named macros** in the pinned header,
including the three aliases `POSIX_EGREP`, `ED`, and `SED`. The roadmap's
candidate list counted `Egrep / PosixEgrep` together; this inventory records
both names separately.

## Consequences and closure

- GNU Basic, Extended, and Emacs are current bundles of coupled choices plus
  `IgnoreCase`, `NewLineSensitive`, `DotMatchesNull`,
  `AllowInvalidRepetitionOperators`, and `AllowEmptyRanges`. None is a
  one-to-one representation of a named Gnulib configuration.
- Preserve current results as characterization fixtures. Add differential
  cases for each coupled or missing control before any normalization; especially
  anchor contexts, pattern newline, backslash in brackets, invalid intervals,
  unmatched parentheses, and capture suppression.
- The future `Icod.Expressions.Gnu` profile names must preserve the pinned
  compositions. `GnuRegexProfiles.PosixBasic` and `.PosixExtended` must stay
  distinct from strict `Icod.Expressions.Bre` and `.Ere` contracts.
- A03 will fill the known-consumer column with source-pinned call sites;
  the entries above identify likely consumers, not an exhaustive dependency
  trace. A02's acceptance gate remains open pending tests and exact managed
  profile parity.

The [non-destructive migration rule](Icod.Expressions-A00-In-Situ-Inventory.md#non-destructive-migration-rule)
still applies: no removal or behavioral refactor of the CommandFramework
engine during this audit.
