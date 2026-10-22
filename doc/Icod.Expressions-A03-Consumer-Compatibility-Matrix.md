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
| `Icod.CoreUtils/nl` [style parser](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/nl/src/Command.cs#L181-L185) [and compilation](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/nl/src/Command.cs#L284-L298) | GNU Basic with `GnuExprCompatibility`; C-locale provider for byte mode, current-culture Unicode provider otherwise; compiles the text after `p` in numbering style. | Compiled expression is held by `NlNumberingStyle`; its per-line match/decoding path and diagnostic formatting require a focused follow-up trace. Numbering policy remains layer 4. |
| `Icod.CoreUtils/csplit` [control compilation](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/csplit/src/Command.cs#L940-L976) [and search](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/csplit/src/Command.cs#L635-L654) | GNU Basic `GnuExprCompatibility`; default provider, per-line byte `Match` with default byte input/match options. | Searches succeeding lines, treating any match as a split-control hit; `/` and `%` delimiters and numeric offsets are csplit policy. Compile errors become usage exceptions; match errors become I/O exceptions. |
| `Icod.CoreUtils/ptx` [profile](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/ptx/src/PtxPatterns.cs#L35-L67) [and iteration](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/ptx/src/PtxPatterns.cs#L75-L215) | GNU Emacs provider and `GnuEmacsCompatibility`, `IgnoreCase` from settings, `NewLineSensitive=false`, injected C-locale classifier. Prepared one-byte input normally; a bounded word check decodes a prefix as Latin-1 and uses `RequireMatchAtStart=true` on `string`. | Advances one byte after empty word match; rejects empty custom sentence separator. Bounded word behavior depends on the truncated input, so a future prepared-input optimization must retain the bound. Word/sentence segmentation and error text remain layer 4. |
| `Icod.CoreUtils/tac` [compilation](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/tac/src/Command.cs#L190-L212) [and search](https://github.com/uniblab/Icod.CoreUtils/blob/2c47cd67bae490dedf16234d8460c8b9da8d3404/tac/src/Command.cs#L464-L499) | GNU Emacs options passed to `GnuBasicRegularExpressionProvider`; prepared byte input with byte start offsets. Verify this provider/options combination explicitly in characterization. | Skips selected window-edge matches and advances at a UTF-8 boundary while finding the last separator. Window framing, reversal, and separator diagnostics remain layer 4. |
| `Icod.DiffUtils/diff` [options](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/diff/src/Command.cs#L307-L337) | GNU Basic provider, default `Syntax`, `IgnoreCase` from comparison policy and `NewLineSensitive=true`; `string` lines for `-I` and function context. | [Output writer](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/diff/src/DiffOutputWriter.cs) tests line matches; diff's equivalence, hunk and function-context rules remain layer 4. Invalid patterns become `DiffUsageException`. |
| `Icod.DiffUtils/sdiff` [options](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/sdiff/src/Command.cs#L276-L288) | Same GNU Basic options (`IgnoreCase`, `NewLineSensitive=true`) for `-I` on `string` lines. | [Comparison builder](https://github.com/uniblab/Icod.DiffUtils/blob/8192ec43e1f6add42bc759b225c814861e18c843/sdiff/src/SDiffComparisonBuilder.cs) tests line matches; line comparison and presentation remain layer 4. Invalid patterns become `SDiffUsageException`. |
| `Icod.LineEditor/sed` [compiler](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/sed/src/SedRegularExpressions.cs#L159-L265) [and matches](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/sed/src/SedRegularExpressions.cs#L53-L145) | GNU Basic/Extended provider, `Syntax`, `IgnoreCase`, `NewLineSensitive=M`, `LineSeparator=NUL` for `-z` else LF, `DotMatchesNull=-z`. C-locale classifier for byte decoding, current-culture Unicode otherwise. Sed builds a `string` pattern space. | Uses `StartIndex` for repeated matches, skips a zero-length match immediately following a nonempty match at the same endpoint, advances by a Unicode scalar on empty matches. `--posix` escape expansion/normalization, empty-pattern reuse, addresses, substitutions/replacement syntax, and script diagnostics remain layer 4. The general separator and dot policies are layer 3. |
| `Icod.LineEditor/ed` and `/red` [host selection](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/ed/src/Command.cs#L216-L220) [shared engine](https://github.com/uniblab/Icod.LineEditor/blob/1705e1dc5107eb2ede6b5ecec5dad7ea18c93061/Icod.LineEditor.Ed.Shared/src/EditorEngine.cs#L891-L1001) | GNU Basic or Extended provider depending on command flag, compiled with provider defaults; `string` line inputs. | Shared Ed engine owns remembered empty pattern, line-address wrapping, global selection, substitution and replacement (`&`, `\\1`–`\\9`), occurrence/global flags, and one-character advance after empty match. `red` additionally owns restricted capabilities. Matcher result/captures remain layer 3; editor behavior remains layer 4. |

## Migration and closure checks

1. Extract a copy of the provider, options, matching, diagnostics, and byte
   preparation contract into `Icod.Expressions` and compare results against
   the frozen CommandFramework engine. In particular retain the mixed
   provider/Emacs-options invocation in `tac` until its behavior is understood.
2. Keep `Icod.CommandFramework` source and public entry points operational.
   Change no dependency or forwarding path until the new library has an
   implementation with parity evidence and each affected consumer can build
   and run against it; remove old code only in a separate reviewed phase.
3. Add consumer-shaped fixtures for grep byte offsets and empty matches,
   ptx's bounded anchored match, expr's first capture, sed's NUL separator
   and `M`, and Ed's capture-based replacement. Include diff line matching
   and procps string offsets in migration validation.
4. Finish the `nl` per-line invocation and inspect any regex callers outside
   these source-pinned default-branch search results. Capture each consumer's
   test/command oracle and package version before declaring A03 closed.

This matrix classifies discovered call sites, not a final certification that
all repositories or application-specific edge cases have been exhausted.
