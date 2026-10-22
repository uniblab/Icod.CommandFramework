# Icod.Expressions A01: POSIX BRE/ERE semantic audit

**Status:** Draft in-situ audit, not a strict POSIX conformance claim. This
document classifies the existing engine at CommandFramework commit
`e1e118f18246eed290bee4758325a20e302bfb92`. No production behavior is
changed. The standards authority is POSIX.1-2024 Issue 8, document
[`9799919799`, chapter 9](https://pubs.opengroup.org/onlinepubs/9799919799/basedefs/V1_chap09.html).
The existing GNU behavior is separately pinned to Gnulib commit
`fb7312fa8d3df29f0ca0678f669b9a5b88a078ec` in the
[CoreUtils upstream ledger](https://github.com/uniblab/Icod.CoreUtils/blob/main/Icod.CoreUtils-Upstream-Version-Ledger.md).

This is an inventory of **observed source and test contracts**. The column
called "classification" identifies the work needed to separate a future
strict POSIX surface from the current GNU surface. It does not infer that a
case passes an external POSIX oracle just because a local test passes.

## Current entry points

`RegularExpressionOptions.Syntax` defaults to `Basic`. The Basic, Extended,
and Emacs providers all use `GnuBasicRegularExpressionParser` and the same
leftmost-longest matcher. Basic and Extended are currently described as
"GNU/POSIX" in `src/RegularExpressions/README.md`; they are not separately
named strict POSIX contracts. `RegularExpressionOptions` exposes GNU
compatibility presets and language-affecting switches to every provider.

## Semantic classification

| Area | Observed implementation and evidence at the pinned commit | Classification / characterization needed |
|---|---|---|
| BRE spelling | Escaped grouping, alternation, `+`, `?`, and intervals; `*` unescaped. `GnuBasicRegularExpressionParser.cs` selects operator length by syntax, and `GnuBasicRegularExpressionTests.cs` exercises all forms. | Separate the standard BRE subset from GNU escaped alternation and repetition extensions. Do not call the current entire Basic profile strict POSIX. |
| ERE spelling | Unescaped grouping, alternation, `+`, `?`, intervals; escaped metacharacters become literals. `GnuExtendedRegularExpressionTests.cs` covers these forms. | Audit each case against chapter 9 grammar and identify GNU interpretation of invalid or undefined syntax separately. |
| Backreferences | `\1` through `\9` target already closed captures in both Basic and Extended parser modes; Basic and Extended tests cover them. | BRE backreferences require standards fixtures. The ERE acceptance is GNU behavior, not a portable ERE promise; freeze it as a divergence fixture. |
| Intervals | The parser accepts `{m}`, `{m,}`, `{m,n}`, plus omitted lower bounds such as `{,n}`; it caps bounds at 32,767 and has context-dependent invalid-operator handling. | Partition standard forms and implementation/GNU extensions. Test boundary values, leading zeros, malformed forms, and invalid-context dispositions per syntax. |
| Whole-match choice | `GnuBasicCompiledRegularExpression.Search` enumerates starts and selects the greatest endpoint at the first viable start. `SearchSelectsTheLongestMatchAtTheLeftmostPosition` and ERE/LineEditor tests exercise it. | Retain leftmost-longest characterization, then compare captures and tie behavior with the standards oracle rather than assuming the whole-match test proves subexpression selection. |
| Captures and repetition | Groups record opening-order numbers; repeated groups retain last participating values. Basic tests exercise nested, empty, and nonparticipating captures and equal-endpoint GNU ordering. | Build a separate strict POSIX capture/tie matrix. Preserve current GNU result as an inherited baseline even where normalization later differs. |
| Anchors | `^` is recognized at a branch start and `$` at a branch end. `NewLineSensitive` and `LineSeparator` alter line boundaries; Basic and LineEditor tests cover line-sensitive behavior. | Separate common expression grammar from utility-specific line policy; characterize anchors after a start offset and within alternation/grouping. |
| Dot and nonmatching lists | Dot excludes NUL by default in Basic/Extended; line sensitivity excludes the configured separator. GNU Emacs has its own dot behavior. A negated bracket excludes the separator when line-sensitive. | Record NUL/newline behavior for the strict standards contract separately from GNU Emacs and consumer-selected separators. |
| Bracket forms | Parser supports classes, ranges, first `]`/edge `-`, and single-scalar collating/equivalence elements. `[[.ch.]]` returns `UnsupportedCollatingElement`; unknown classes and reverse ranges have diagnostics. | Single-scalar support is partial for locales with multicharacter collating elements. Mark complete locale-collation conformance unproven; characterize class names and range order under injected providers. |
| Locale | Two providers implement Unicode/culture and invariant C-locale classification, scalar comparison, and equivalence. `IRegularExpressionCharacterClassProvider` is injectable; tests prove selected provider behavior. | An injected scalar provider is an implementation seam, not proof of full POSIX locale semantics. Audit `LC_CTYPE`, `LC_COLLATE`, multicharacter elements, case folding, and equivalence independently. |
| String, bytes, UTF-8 | String matching evaluates scalar units but reports UTF-16 indices. Byte mode uses one unit per source byte; UTF-8 mode retains source-byte offsets. Split-scalar start positions fail with controlled diagnostics. | These are valuable managed API policies; classify them apart from POSIX C string/locale semantics. Test both widths and offsets in the frozen corpus. |
| Malformed input | Unpaired UTF-16 is evaluated as U+FFFD; invalid UTF-8 policy is `PreserveBytes`, `Replace`, or `Throw`. Bytes remain authoritative in byte results. | Preserve the current behavior in characterization; its standards status depends on the selected encoding/locale and is not a universal POSIX guarantee. |
| Operational limits | Nesting and match-state limits produce controlled diagnostics; cancellation throws. Tests cover these paths. | Managed operational policy. Record exact results for extraction parity, not as syntax conformance. |

## Findings that affect the architecture

1. The current `Basic`/`Extended` selector is a **GNU/POSIX family switch**,
   not a sufficient strict-POSIX opt-in. A01 must describe strict BRE/ERE
   contracts without silently changing existing provider behavior.
2. `GnuExtendedRegularExpressionTests.CapturesAndBackReferencesUseExtendedGrouping`
   makes ERE backreferences an explicit inherited GNU behavior. A future
   strict ERE surface needs its own decision and tests.
3. Multi-scalar collating elements cannot be represented by the present
   bracket term and provider model. The source reports a controlled error;
   any strict POSIX completeness claim must state this boundary.
4. Current tests establish many local behaviors but do not form a
   standards-versus-GNU divergence corpus. The same result under one
   provider and locale does not prove all permitted locales.

## A01 closure work

- Record the exact relevant chapter 9 subsection for each standard case and
  distinguish required, unspecified/undefined, GNU-defined, and unsupported
  behavior. Avoid treating a GNU extension as a POSIX requirement.
- Add or identify focused fixtures for pure BRE/ERE syntax, GNU divergences,
  leftmost-longest subexpression choices, anchors, bracket/locale behavior,
  backreferences, byte/UTF-8/UTF-16 coordinates, malformed input, and limits.
- Run a pinned standards/Gnulib differential oracle where feasible and
  classify each disagreement. Record executable version and locale for every
  runtime comparison.
- Keep production source unchanged during A01-A04. In particular, the
  [A00 migration rule](Icod.Expressions-A00-In-Situ-Inventory.md#non-destructive-migration-rule)
  keeps the CommandFramework engine intact while the new library is developed
  and proven.
