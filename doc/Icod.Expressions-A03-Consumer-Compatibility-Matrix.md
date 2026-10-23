# Icod.Expressions A03: consumer compatibility matrix

**Status:** Source-pinned in-situ audit draft. This traces known call sites in
five repositories; it does not change any consumer or the CommandFramework
engine. Repository references are the default-branch commits returned by the
GitHub code index on 2026-10-22:

| Repository | Inspected source commit |
|---|---|
| `Icod.CoreUtils` | [`2c47cd67bae490dedf16234d8460c8b9da8d3404`](https://github.com/uniblab/Icod.CoreUtils/tree/2c47cd67bae490dedf16234d8460c8b9da8d3404) |
| `Icod.Grep` | [`2ef1c11f11b77eb876242eb0c92a88d20857db64`](https://github.com/uniblab/Icod.Grep/tree/2ef1c11f11b77eb876242eb0c92a88d20857db64) |
| `Icod.ProcPs` | [`e802ac03d941b1ba9fbe30e8f9c68fc7efcf3cff`](https://github.com/uniblab/Icod.ProcPs/tree/e802ac03d941b1ba9fbe30e8f9c68fc7efcf3cff) |
| `Icod.DiffUtils` | [`8192ec43e1f6add42bc759b225c814861e18c843`](https://github.com/uniblab/Icod.DiffUtils/tree/8192ec43e1f6add42bc759b225c814861e18c843) |
| `Icod.LineEditor` | [`1705e1dc5107eb2ede6b5ecec5dad7ea18c93061`](https://github.com/uniblab/Icod.LineEditor/tree/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061) |

Here *GNU* identifies the existing `GnuBasicRegularExpressionProvider`,
`GnuExtendedRegularExpressionProvider`, or `GnuEmacsRegularExpressionProvider`.
None of these call sites opts into a separately named strict POSIX contract.
The Gnulib named-profile mapping remains an A02 differential task; a current
convenience preset is not proof of bit-for-bit `RE_SYNTAX_*` parity.

## Call-site and application-policy inventory

| Consumer and source | Layer-3 syntax/options and input path | Match, locale, diagnostics, and layer-4 ownership |
|---|---|---|
| `Icod.Grep` [command lines 1075–1173](https://github.com/uniblab/Icod.Grep/blob/2ef1c11f11b77eb876242eb0c92a88d20857db64/src/Command.cs#L1075-L1173) | GNU Extended uses `GnuExtendedCompatibility`; GNU Basic supplies `Syntax=Basic`, `AllowInvalidRepetitionOperators=true`. Both set `IgnoreCase` and `NewLineSensitive=!NullData`. Injects the selected locale's class provider. Byte input specifies locale `DecodingMode` and `PreserveBytes`; may prepare the byte input. PCRE and fixed-string modes use separate implementations. | [Regex pattern lines 203–229](https://github.com/uniblab/Icod.Grep/blob/2ef1c11f11b77eb876242eb0c92a88d20857db64/src/Command.cs#L203-L229) passes `StartByteOffset`, returns byte spans; pattern set handles repeated searching, multiple-pattern selection, word/line filtering, empty matches, records, and output. Compile errors flow through grep diagnostics; match errors become command exceptions. All those command rules remain layer 4. |
| `Icod.ProcPs` [`GnuProcMatchPatternCompiler`](https://github.com/uniblab/Icod.ProcPs/blob/e802ac03d941b1ba9fbe30e8f9c68fc7efcf3cff/build/GnuProcMatchPatternCompiler.cs#L27-L79) | GNU Extended preset plus `IgnoreCase`; default provider and `string` input. | Unanchored `Match(input)` with no explicit start offset; converts UTF-16 match index/length to `ProcMatchPatternResult`. Invalid compile raises `FormatException`; non-success match returns null. Process selection and command error presentation remain layer 4. |
| `Icod.CoreUtils/expr` [evaluator lines 296–347](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/expr/src/ExpressionEvaluator.cs#L296-L347) | GNU Basic provider (injectable through [command](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/expr/src/Command.cs#L86-L96)); `GnuExprCompatibility`; `string` input. | `RequireMatchAtStart=true`; a successful first capture returns its text, otherwise returns matched length or empty/zero according to `expr` evaluation. Compile and match diagnostics become expression command failures. Captured-result choice and status codes remain layer 4; anchored matching and capture fidelity remain layer 3. |
| `Icod.CoreUtils/nl` [style parser](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/nl/src/Command.cs#L181-L185) [and compilation](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/nl/src/Command.cs#L284-L298) | GNU Basic with `GnuExprCompatibility`; C-locale provider for byte mode, current-culture Unicode provider otherwise; compiles the text after `p` in numbering style. | [Processor](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/nl/src/NlProcessor.cs#L156-L190) decodes each line to `string` and calls `MatchAsync` without an explicit start offset or anchor; any match numbers the line. Match diagnostics become `NumberDecision` failure text. Numbering and line decoding remain layer 4. |
| `Icod.CoreUtils/csplit` [control compilation](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/csplit/src/Command.cs#L940-L976) [and search](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/csplit/src/Command.cs#L635-L654) | GNU Basic `GnuExprCompatibility`; default provider, per-line byte `Match` with default byte input/match options. | Searches succeeding lines, treating any match as a split-control hit; `/` and `%` delimiters and numeric offsets are csplit policy. Compile errors become usage exceptions; match errors become I/O exceptions. |
| `Icod.CoreUtils/ptx` [profile](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/ptx/src/PtxPatterns.cs#L35-L67) [and iteration](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/ptx/src/PtxPatterns.cs#L75-L215) | GNU Emacs provider and `GnuEmacsCompatibility`, `IgnoreCase` from settings, `NewLineSensitive=false`, injected C-locale classifier. Prepared one-byte input normally; a bounded word check decodes a prefix as Latin-1 and uses `RequireMatchAtStart=true` on `string`. | Advances one byte after empty word match; rejects empty custom sentence separator. Bounded word behavior depends on the truncated input, so a future prepared-input optimization must retain the bound. Word/sentence segmentation and error text remain layer 4. |
| `Icod.CoreUtils/tac` [compilation](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/tac/src/Command.cs#L190-L212) [and search](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/tac/src/Command.cs#L464-L499) | GNU Emacs options passed to `GnuBasicRegularExpressionProvider`; that provider [forcibly replaces `Syntax` with `Basic`](https://github.com/uniblab/Icod.CommandFramework/blob/e1e118f18246eed290bee4758325a20e302bfb92/src/RegularExpressions/GnuBasicRegularExpressionProvider.cs#L24-L29). The resulting syntax is Basic, with other supplied option fields retained. Prepared byte input uses byte start offsets. This is a **known consumer discrepancy** relative to the apparent Emacs intent, now pinned by an A04 test. | Skips selected window-edge matches and advances at a UTF-8 boundary while finding the last separator. Window framing, reversal, and separator diagnostics remain layer 4. Do not silently correct this call during extraction; adjudicate it in a separate consumer change. |
| `Icod.DiffUtils/diff` [options](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/diff/src/Command.cs#L307-L337) | GNU Basic provider, default `Syntax`, `IgnoreCase` from comparison policy and `NewLineSensitive=true`; `string` lines for `-I` and function context. | [Output writer](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/diff/src/DiffOutputWriter.cs) tests line matches; diff's equivalence, hunk and function-context rules remain layer 4. Invalid patterns become `DiffUsageException`. |
| `Icod.DiffUtils/sdiff` [options](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/sdiff/src/Command.cs#L276-L288) | Same GNU Basic options (`IgnoreCase`, `NewLineSensitive=true`) for `-I` on `string` lines. | [Comparison builder](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/sdiff/src/SDiffComparisonBuilder.cs) tests line matches; line comparison and presentation remain layer 4. Invalid patterns become `SDiffUsageException`. |
| `Icod.LineEditor/sed` [compiler](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/sed/src/SedRegularExpressions.cs#L159-L265) [and matches](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/sed/src/SedRegularExpressions.cs#L53-L145) | GNU Basic/Extended provider, `Syntax`, `IgnoreCase`, `NewLineSensitive=M`, `LineSeparator=NUL` for `-z` else LF, `DotMatchesNull=-z`. C-locale classifier for byte decoding, current-culture Unicode otherwise. Sed builds a `string` pattern space. | Uses `StartIndex` for repeated matches, skips a zero-length match immediately following a nonempty match at the same endpoint, advances by a Unicode scalar on empty matches. `--posix` escape expansion/normalization, empty-pattern reuse, addresses, substitutions/replacement syntax, and script diagnostics remain layer 4. The general separator and dot policies are layer 3. |
| `Icod.LineEditor/ed` and `/red` [host selection](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/ed/src/Command.cs#L216-L220) [shared engine](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/Icod.LineEditor.Ed.Shared/src/EditorEngine.cs#L891-L1001) | GNU Basic or Extended provider depending on command flag, compiled with provider defaults; `string` line inputs. | Shared Ed engine owns remembered empty pattern, line-address wrapping, global selection, substitution and replacement (`&`, `\\1`–`\\9`), occurrence/global flags, and one-character advance after empty match. `red` additionally owns restricted capabilities. Matcher result/captures remain layer 3; editor behavior remains layer 4. |

## Shared Text enum forwarding boundary

The extracted regex input options use `TextDecodingMode` and
`InvalidEncodingPolicy`. Those public enum identities also occur outside
regex calls. In the pinned CoreUtils source, `cut`, `expand`, `unexpand`,
`fold`, and `fmt` call CommandFramework Text APIs that use one or both enums;
CoreUtils Shared has additional direct Text API callers. A CommandFramework
bridge must forward the enum identities while keeping `TextUnitReader` and
the other non-regex Text implementation in CommandFramework.

The [Expressions three-platform scratch gate](https://github.com/uniblab/Icod.Expressions/blob/bootstrap/pre-extraction-foundation/doc/E03-Compatibility-Bridge.md#cross-platform-pinned-consumer-gate)
now adds the original `cut` (28), `expand` (18), `unexpand` (20), `fold` (18),
and `fmt` (29) suites to the 611 regex-consumer cases: **724 source-pinned
cases per pass**. Each command's original and bridged binaries pass with the
same test names and outcomes on Windows, macOS, and Linux
([CI run 40014111537](https://github.com/uniblab/Icod.Expressions/actions/runs/40014111537)).
An old-compiled assembly probe checks that `TextDecodingMode` and
`InvalidEncodingPolicy` resolve from Expressions while `TextUnitReader`
resolves from the scratch CommandFramework bridge. This does not cover every
standalone CoreUtils Shared Text test, every Text API caller, or a
distributable bridge.

## Migration and closure checks

Five additive engine-level cases for Grep, ProcPs, Ptx, Sed, and Ed are
recorded in the [A04 supplement](Icod.Expressions-A04-Consumer-Shape-Supplement.md).
They characterize caller-shaped inputs and offsets, not command output or
full consumer integration.

A later [scratch-only bridge probe in Expressions](https://github.com/uniblab/Icod.Expressions/blob/bootstrap/pre-extraction-foundation/doc/E03-Compatibility-Bridge.md#source-pinned-coreutils-expr-command-probe)
compiled the source-pinned CoreUtils `expr` command tests against the
published CommandFramework 2.2.1 package, then ran the compiled binaries
with a temporary non-regex CommandFramework bridge and the copied Expressions
assembly. Its 50 existing command tests passed; an additional assembly-origin
assertion and a missing-DLL negative control confirmed the forwarded provider
was loaded from Expressions. This is one consumer on Linux/.NET 10, not a
production bridge or closure of the remaining A03 matrix.

Expressions now also runs a
[reproducible package-backed Linux gate](https://github.com/uniblab/Icod.Expressions/blob/bootstrap/pre-extraction-foundation/doc/E03-Compatibility-Bridge.md#reproducible-package-backed-consumer-gate).
It fetches pinned framework, CoreUtils, ProcPs, and DiffUtils commits, compiles the
original `expr` tests against CommandFramework 2.2.1 and ProcPs's `pgrep`,
`pkill`, and `pidwait` plus DiffUtils's `diff` and `sdiff` suites against 2.1.0,
and runs all 136 original cases.
It builds the temporary 2.2.1.0 bridge against the unpublished Expressions
package from an isolated NuGet cache, verifies forwarding identity, and
runs the same 50 + 11 + 7 + 6 + 22 + 40 compiled tests in separate scratch outputs.
The local Linux/.NET 10 gate passes both runs. It does not change or merge
CommandFramework production, prove a distributable bridge, or close the
remaining A03 consumer matrix. The ProcPs tests cover command matching paths,
but do not exhaust GNU Extended options or diagnostics. The `sdiff` tests include
ignored-line `-I` cases, and the `diff` tests include `-F` function-context
matching. A direct `diff -I` command case remains open under
[DiffUtils #8](https://github.com/uniblab/Icod.DiffUtils/issues/8).

The gate now covers pinned Grep commit
`2ef1c11f11b77eb876242eb0c92a88d20857db64`, which compiles its original
test project against CommandFramework 2.2.1. Its 112 tests other than a
device-policy Unix-domain socket test pass locally with both the published
package and scratch bridge; this workspace denies socket creation. The
hosted [Linux gate 39888672536](https://github.com/uniblab/Icod.Expressions/actions/runs/39888672536)
ran all 113 without that local filter and passed before and after bridge
substitution. Grep's byte offset and prepared-input paths are present in
that original suite.

The next extension pins LineEditor commit
`1705e1dc5107eb2ede6b5ecec5dad7ea18c93061`. Its original Sed,
Ed.Shared, Ed, and Red suites compile against published CommandFramework
2.1.0. All 116 + 68 + 26 + 28 tests pass locally against that package and
with the scratch bridge substituted. [Expressions CI run 39889627829](https://github.com/uniblab/Icod.Expressions/actions/runs/39889627829)
passed the full 487-case gate, including these 238 LineEditor cases. These
command tests exercise substitution, repeated searches, and
shared editor regex paths; additional caller audit and cross-platform
forwarding remain open.

The pinned CoreUtils checkout also has `NL`, `CSplit`, and `Ptx` command
suites (35 + 43 + 46 cases). Their GNU Basic pattern styles, split controls,
and prepared byte input pass locally against the published package and the
scratch bridge. [Expressions CI run 39890569950](https://github.com/uniblab/Icod.Expressions/actions/runs/39890569950)
passed the complete 611-case hosted gate, including those CoreUtils commands.
`tac` remains separate under
[CoreUtils #190](https://github.com/uniblab/Icod.CoreUtils/issues/190).

An additional default-branch code-search check on 2026-10-23 searched the
`uniblab` owner for `Icod.CommandFramework.RegularExpressions` (95 indexed
files), `GnuBasicRegularExpressionProvider` (32), and
`GnuEmacsRegularExpressionProvider` (6). The repository names in these
results were CommandFramework and the same five consumer repositories in
this matrix; no sixth repository appeared. This is bounded to indexed
default-branch matches for those names. Aliases, reflection, unindexed files,
and other branches still require a separate caller audit before A03 closure.

1. Extract a copy of the provider, options, matching, diagnostics, and byte
   preparation contract into `Icod.Expressions` and compare results against
   the frozen CommandFramework engine. Preserve the Basic-provider override
   observed at the `tac` call site for parity, then make any intended Emacs
   correction separately with consumer tests and review.
2. Keep `Icod.CommandFramework` source and public entry points operational.
   Change no dependency or forwarding path until the new library has an
   implementation with parity evidence and each affected consumer can build
   and run against it; remove old code only in a separate reviewed phase.
3. Add consumer-shaped fixtures for grep byte offsets and empty matches,
   ptx's bounded anchored match, expr's first capture, sed's NUL separator
   and `M`, and Ed's capture-based replacement. Include diff line matching
   and procps string offsets in migration validation.
4. Inspect any regex callers outside these source-pinned default-branch search
   results. Capture each consumer's test/command oracle and package version
   before declaring A03 closed.

The `tac` discrepancy is tracked as
[Icod.CoreUtils issue #190](https://github.com/uniblab/Icod.CoreUtils/issues/190)
for a later consumer bugfix, after the new library has a solid implementation.
This matrix classifies discovered call sites, not a final certification that
all repositories or application-specific edge cases have been exhausted.
