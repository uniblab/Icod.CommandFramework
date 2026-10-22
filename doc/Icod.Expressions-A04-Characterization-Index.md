# Icod.Expressions A04: in-situ characterization index

**Status:** Working coverage index, not a conformance or extraction freeze.
The production regex files and two external text enums are unchanged from
CommandFramework commit `e1e118f18246eed290bee4758325a20e302bfb92`.
The first ten focused test files at that commit have Git blob IDs in the
[A00 manifest](Icod.Expressions-A00-In-Situ-Inventory.md#complete-tracked-sourcetest-manifest).
The additive `IcodExpressionsA04CharacterizationTests.cs` has Git blob
`bc93fdf45c92ed18b2a883dca9f0e0f80f9f5c0c`.

There are **109 `[Fact]`/`[Theory]` declarations** in the current focused
regex test directory: 106 at the pinned source commit and three added by this
audit. Theory rows expand to more runtime cases. The five runtime cases from
the additive file passed locally on .NET SDK 10.0.402 and in the
[three-platform PR workflow](https://github.com/uniblab/Icod.CommandFramework/actions/runs/39848729579).
This index points to assertions already present; it does not imply that each
feature is fully tested against an external oracle.

## Evidence already in the corpus

| Contract area | Focused assertions | What they establish |
|---|---|---|
| Basic spelling and GNU extensions | `GnuBasicRegularExpressionTests`: `RepetitionOperatorsUseGnuBasicSyntax`, `ContextuallyInvalidRepetitionTokensAreOrdinary`, `StrictBasicSyntaxAcceptsGnuValidAdjacentRepetitions`, `ExprCompatibilityAcceptsGnulibInvalidDuplicateProfile` | Current Basic parser and expr preset behavior, including accepted contexts that must not be relabeled strict POSIX. |
| Extended spelling | `GnuExtendedRegularExpressionTests`: `ExtendedOperatorsAreUnescaped`, `EscapedExtendedMetacharactersAreLiterals`, `MalformedExtendedBraceTextRemainsLiteral`, `GnuExtendedCompatibilityAcceptsInvalidDuplicateContexts` | Current Extended spelling and malformed-brace policy, not the complete Gnulib syntax-bit set. |
| Emacs spelling | `GnuEmacsRegularExpressionTests`: `PlusAndQuestionMarkAreUnescapedOperators`, `GroupingAlternationAndIntervalsRemainEscaped`, `DotMatchesNullButNotLineFeed`, `BackslashDoesNotEscapeClosingBracket` | Current Emacs provider behavior used by ptx. |
| Source/profile coupling found in A02/A03 | `IcodExpressionsA04CharacterizationTests`: `BasicProviderOverridesEmacsSyntaxRequestedByTac`, `PatternNewlineRemainsLiteralInCurrentBasicAndExtendedProviders`, `MidBranchCaretAndDollarRemainLiterals` | The Basic-provider override at the tac call shape, lack of pattern-newline alternation, and contextual anchors. The tac bugfix is tracked in [CoreUtils #190](https://github.com/uniblab/Icod.CoreUtils/issues/190). |
| Whole match, captures, backreferences | `GnuBasicRegularExpressionTests`: `SearchSelectsTheLongestMatchAtTheLeftmostPosition`, `OverallLongestMatchConstrainsSubexpressionSelection`, `EqualLengthMatchesFollowGnuGreedyCaptureOrdering`, `EmptyAndNonparticipatingCapturesRemainDistinct`; `GnuExtendedRegularExpressionTests`: `CapturesAndBackReferencesUseExtendedGrouping` | Internal GNU leftmost-longest and capture choices; ERE backreference acceptance is a GNU fixture, not a strict ERE guarantee. |
| Anchors, dot, NUL, line separator | `GnuBasicRegularExpressionTests`: `AnchorsHonorNewLineSensitivePolicy`, `DotMatchesNewlineButNeverNullByDefault`, `DotAndNegatedBracketExcludeNewlineWhenRequested`; `LineEditorRegularExpressionContractTests`: `ConsumerSelectedLineSeparatorControlsMultilineAndDotPolicy` | Current managed line policy, including Sed's NUL separator choice. |
| Brackets and locale | `GnuBasicRegularExpressionTests`: `PosixClassesRangesAndEquivalenceExpressionsAreSupported`, `EmptyRangeCompatibilityCanBeEnabledForExpr`, `CharacterClassesAreProviderDrivenAndInjectable`; `GnuExtendedRegularExpressionTests`: `CharacterClassesUseTheInjectedLocaleProvider` | Selected scalar classes, ranges, equivalence behavior and provider injection, not multicharacter collation completeness. |
| String, byte and UTF-8 coordinates | `LineEditorRegularExpressionContractTests`: `StringAndUtf8ByteSurfacesReturnAuthoritativeCoordinates`, `Utf8StartOffsetCannotSplitASourceScalar`, `MalformedUtf8CanRemainAnExactAuthoritativeByte`; `GnuExtendedRegularExpressionTests`: `ByteModeUsesOneMatchingUnitPerSourceByte`, `InvalidUtf8PolicyIsExplicitAndBytePreserving` | UTF-16 string indices, authoritative byte slices/offsets, and controlled split-scalar failures. |
| Prepared input and optimization parity | `PreparedRegularExpressionTests`, `PreparedRegularExpressionProfileTests`, `PublicPreparedByteInputTests`, `PublicPreparedByteInputIsolationTests`, `LiteralPrefixRegularExpressionTests`, and `R2_4RegularExpressionClosureTests` | Prepared/ordinary byte parity, source ownership, concurrency, malformed input, prefix fallback, resource accounting, and cancellation in selected cases. |
| Diagnostics and limits | `GnuBasicRegularExpressionTests`: `InvalidPatternsProduceStableDiagnostics`, `ConfiguredNestingLimitProducesAControlledDiagnostic`, `ConfiguredMatchStateLimitProducesAControlledFailure`, `CompileAndMatchHonorCancellation`; `GnuExtendedRegularExpressionTests`: `InvalidExtendedPatternsProduceStableDiagnostics` | Existing controlled result and cancellation paths. |

## Required closure cases still open

| Priority | Gap | Next executable evidence |
|---|---|---|
| A01 | Strict BRE and strict ERE are not independently selectable APIs. Some malformed or escaped constructs are undefined or GNU-defined rather than standard-required. | Write separate draft `POSIX-BRE-CONTRACT.md` and `POSIX-ERE-CONTRACT.md` with section-level Issue 8 references; record standard status per fixture. Compare only defined cases to a pinned POSIX oracle and mark undefined cases without forcing one result. |
| A01 | Subexpression tie ordering, BRE backreference edge cases, and locale-sensitive bracket behavior need independent comparison. Multi-scalar collating elements are not representable in the current bracket model. | Add a differential table with pattern, input, locale, expected capture spans, oracle identity, and disposition; keep controlled `UnsupportedCollatingElement` as an inherited behavior fixture. |
| A02 | The current parser offers coupled Basic/Extended/Emacs modes, not switches for all 26 pinned Gnulib bits or 13 named macros. | For each implemented/coupled bit, test both polarities where meaningful; for missing bits, record a fixture as unsupported rather than pretending a profile is present. Compare named configurations with pinned Gnulib `regex.h`/engine. |
| A03 | Command-level invocation and output remain outside the focused CommandFramework test assembly. | Run source-pinned consumer tests for grep byte/UTF-8 offsets and empty matches, expr anchored capture return, ptx bounded search, Sed `-z`/`M` and replacement, Ed global substitution, diff line suppression, and procps offsets. Preserve tac's current result for extraction; fix it later under CoreUtils #190. |
| A04 | No pinned external oracle is wired into this test suite. The 109 declarations are local assertions, not 109 standards fixtures. | Record executable versions, source revisions, locale, encoding, invocation, and normalized captures for POSIX/Gnulib differential fixtures. Classify each disagreement as bug, deliberate deviation, undefined/unspecified, or unresolved. |
| A05 | The final extraction source SHA and exact test corpus have not been frozen. | After A01–A04 closure, freeze source/test blob identities, compiled API snapshot, known deviations, and passing CI run; copy source/tests to `Icod.Expressions` without removing CommandFramework code. |

## Reproduction

```sh
rg -o '\[(Fact|Theory)\]' tests/CommandFramework.Tests/src/RegularExpressions -g '*.cs' | wc -l
dotnet test tests/CommandFramework.Tests/Icod.CommandFramework.Tests.csproj \
  --filter FullyQualifiedName~IcodExpressionsA04CharacterizationTests
```

The complete CommandFramework suite is exercised by PR CI on Linux, macOS,
and Windows. The independent oracle and full consumer-level evidence above
remain explicit gates before the extraction baseline is called solid.
