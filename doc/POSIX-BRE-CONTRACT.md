# Icod.Expressions: draft strict POSIX BRE contract

**Status:** Standards-facing design draft for A01; no strict BRE provider exists
yet. Authority: [POSIX.1-2024 Issue 8, Base Definitions chapter 9,
especially §§9.1–9.3](https://pubs.opengroup.org/onlinepubs/9799919799/basedefs/V1_chap09.html).
The source inventory and current behavior are pinned to CommandFramework
`e1e118f18246eed290bee4758325a20e302bfb92`. This draft does not
certify the current GNU Basic provider as conforming.

## Proposed boundary

`Icod.Expressions.Bre` will name a strict standards-facing grammar and
matching contract. It must be a separately selectable surface from
`Icod.Expressions.Gnu` and from the inherited
`GnuBasicRegularExpressionProvider`. The Gnulib macro
`RE_SYNTAX_POSIX_BASIC` is a GNU syntax configuration; its name alone does not
make it this contract. String/byte overloads, source coordinates, decoding,
cancellation, and resource limits are managed API policies layered around
the standards-facing language.

| Topic | Issue 8 reference and strict target | Inherited engine / required evidence |
|---|---|---|
| Ordinary and special characters | §9.3 distinguishes context-sensitive BRE special characters and ordinary characters. | The Basic parser handles contextual `*`, dot, brackets, and escapes, but also accepts GNU operators. Separate standard forms from GNU forms with paired fixtures. |
| Grouping and backreferences | §9.3 describes marked subexpressions and numeric backreferences. | Escaped groups and `\\1`–`\\9` are implemented; characterize unset/empty, repeated, nested, and invalid references against the standard. |
| Repetition and intervals | §9.3 specifies `*` and escaped interval spelling and bounds. | Basic accepts escaped `+`, `?`, alternation, and `{,n}` extensions. These must not become strict BRE promises. Record invalid versus undefined contexts per standard before selecting a diagnostic. |
| Anchors | §9.3's BRE anchoring rules control `^` and `$` by context. | Current branch-boundary rule is characterized locally. Test positions within groups, alternation, start offsets, and line-sensitive policy independently. |
| Brackets, classes, and collation | §9.3.5 covers bracket lists, character classes, collating symbols, equivalence classes, and locale-sensitive ranges. | Single-scalar cases exist. The current bracket representation rejects multi-scalar elements; locale completeness is open. A strict completeness claim requires a representational solution or explicit scoped limitation. |
| Search and captures | §9.1 defines the leftmost-longest whole match and subpattern choice. | Current matcher has local leftmost-longest and GNU tie tests. Compare capture spans with an independent oracle, especially equal-endpoint choices and empty versus absent captures. |
| Input and case | §9.2 defines shared RE requirements and utility/interface-dependent newline treatment. | Managed strings use UTF-16 indices; byte and UTF-8 modes preserve source-byte offsets. NUL, malformed input, Unicode case, chosen line separator, and finite budgets are implementation policies that need separate documented guarantees. |

## Classification rule for the differential corpus

Each fixture must record its pattern and input as bytes or Unicode text,
locale, start position, expected whole match and capture spans, applicable
Issue 8 section, and one of: **standard-required**, **standard-invalid**,
**undefined/unspecified**, **GNU-defined**, or **managed policy**. A
standard-invalid input must produce a controlled diagnostic. An undefined
construct must not acquire a claimed portable outcome solely because GNU or
the current implementation chooses one. Locale-sensitive assertions need
the precise locale and provider recorded.

## Open acceptance work

1. Pin an executable standards oracle and locale for defined C/POSIX cases;
   record Issue 8 text/section for each asserted result. A first
   [Linux/glibc C-locale comparison](Icod.Expressions-A04-POSIX-Glibc-Differential.md)
   now covers three defined ASCII BRE cases; broader coverage remains open.
2. Add explicit strict BRE fixtures alongside inherited GNU Basic fixtures,
   including operator spelling, interval boundaries, backreferences, tie
   ordering, brackets and anchors.
3. Resolve multicharacter collation and locale representation before claiming
   broad strict POSIX conformance. Keep unsupported cases visible in the
   deviation matrix.
4. Do not modify or remove CommandFramework behavior while the strict
   surface and extraction parity are still being developed.

See the [A01 audit](Icod.Expressions-A01-POSIX-Semantic-Audit.md),
[A02 GNU matrix](Icod.Expressions-A02-GNU-Syntax-Bit-Matrix.md), and
[A04 characterization index](Icod.Expressions-A04-Characterization-Index.md).
