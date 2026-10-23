# Icod.Expressions A04: in-situ characterization index

**Status:** Working coverage index, not a conformance or extraction freeze.
The production regex files and two external text enums are unchanged from
CommandFramework commit `e1e118f18246eed290bee4758325a20e302bfb92`.
The first ten focused test files at that commit have Git blob IDs in the
[A00 manifest](Icod.Expressions-A00-In-Situ-Inventory.md#complete-tracked-sourcetest-manifest).
The additive `IcodExpressionsA04CharacterizationTests.cs` and the 44-case
JSONL have immutable identities in the [copy-time checkpoint](Icod.Expressions-A05-Extraction-Baseline.md).
The five additive consumer-shaped cases are separately identified in the
[supplement](Icod.Expressions-A04-Consumer-Shape-Supplement.md).

The first checkpoint has **110 `[Fact]`/`[Theory]` declarations**: 106 at
the pinned source commit and four added by this audit. Theory rows expand to
193 runtime cases. The original additive file has seven runtime cases,
including both orders of the empty-anchor alternation. Five later
consumer-shaped declarations bring the current directory to 115 declarations
and 198 runtime cases on .NET 10. The original 193 cases passed locally on
.NET SDK 10.0.402 before the supplement. The earlier
five-case version passed in the
[three-platform PR workflow](https://github.com/uniblab/Icod.CommandFramework/actions/runs/39848729579).
This index points to assertions already present; it does not imply that each
feature is fully tested against an external oracle.

## Copy-time parity baseline

The [44-case JSON Lines baseline](Icod.Expressions-A04-Parity-Baseline.jsonl)
captures the current engine through its public API. Each row includes the
provider, profile, pattern, input and positioning, followed by the compile
diagnostic or match outcome, span, value and participating captures. Byte
results contain authoritative source bytes as hexadecimal text. The
`RegularExpressionParityBaseline` runner uses an invariant culture, the
explicit C-locale or invariant-Unicode character-class provider, and stable
diagnostic codes rather than display messages. Cases include all three
current syntax providers, the tac provider/option mismatch, literal pattern
newlines, leftmost-longest captures, absent versus empty captures, UTF-16 and
UTF-8 offsets, malformed and opaque bytes, prepared input, case folding,
invalid repetition and range policies, nesting and match-state limits, and
selected consumer-shaped calls for grep, expr, ptx, procps, diff, Ed, and
csplit. The existing tests additionally exercise cancellation, asynchronous
calls, and prepared-input concurrency; this finite JSONL baseline does not
serialize those outcomes. It does not assert that the current outcomes are
POSIX or Gnulib conformant.

Regenerate and verify with .NET SDK 10.0.402 (or another .NET 10 SDK):

```sh
dotnet run --project tools/RegularExpressionParityBaseline/RegularExpressionParityBaseline.csproj \
  -c Release > /tmp/Icod.Expressions-parity-actual.jsonl
cmp doc/Icod.Expressions-A04-Parity-Baseline.jsonl /tmp/Icod.Expressions-parity-actual.jsonl
dotnet run --project tools/RegularExpressionParityBaseline/RegularExpressionParityBaseline.csproj \
  -c Release -- --verify doc/Icod.Expressions-A04-Parity-Baseline.jsonl
```

The repository pins LF checkout for this JSONL file in `.gitattributes` so
the byte-for-byte check has the same expected bytes on Windows, macOS, and
Linux. The PR CI matrix invokes `--verify` under Staging on all three.

An [exploratory GNU grep 3.11 probe](Icod.Expressions-A04-GNU-Grep-Probe.md)
records exact binary identity, C-locale commands, and selected whole-match
comparisons. It identifies an Extended middle-anchor profile difference;
GNU grep 3.11 is not the pinned Gnulib oracle and cannot expose capture
registers or the managed diagnostic identity.

A [pinned Gnulib runtime capture comparison](Icod.Expressions-A04-Gnulib-Capture-Comparison.md)
corrects an apparent source-table discrepancy. The upstream table lists absent
captures for four anchor branches but explicitly tolerates a different actual
span. The built included Gnulib engine reports `(0,0)` in all four; both ERE
rows agree with glibc and the inherited managed behavior. The latter has an
additive characterization test.

An [executable Linux/glibc differential](Icod.Expressions-A04-POSIX-Glibc-Differential.md)
compares nine C-locale ASCII BRE/ERE cases, including whole-match and capture
spans. The seven ordinary standards-facing rows agree locally. glibc agrees
with the inherited engine's participating empty capture in both anchor-branch
rows, as does the separate included Gnulib runtime probe. The glibc tool is
run on Linux CI and is an independent implementation sample.

At E00/E01, copy the case list and runner alongside the tests and adapt only
its assembly reference and namespace. Run it against the pinned
CommandFramework source and the extracted source; require byte-identical
JSONL before E02 parity is closed. Record any intentional difference in a
separate reviewed exception ledger instead of rewriting this baseline. The
baseline is a bounded selection of cases, not full A04 differential closure.

## Evidence already in the corpus

| Contract area | Focused assertions | What they establish |
|---|---|---|
| Basic spelling and GNU extensions | `GnuBasicRegularExpressionTests`: `RepetitionOperatorsUseGnuBasicSyntax`, `ContextuallyInvalidRepetitionTokensAreOrdinary`, `StrictBasicSyntaxAcceptsGnuValidAdjacentRepetitions`, `ExprCompatibilityAcceptsGnulibInvalidDuplicateProfile` | Current Basic parser and expr preset behavior, including accepted contexts that must not be relabeled strict POSIX. |
| Extended spelling | `GnuExtendedRegularExpressionTests`: `ExtendedOperatorsAreUnescaped`, `EscapedExtendedMetacharactersAreLiterals`, `MalformedExtendedBraceTextRemainsLiteral`, `GnuExtendedCompatibilityAcceptsInvalidDuplicateContexts` | Current Extended spelling and malformed-brace policy, not the complete Gnulib syntax-bit set. |
| Emacs spelling | `GnuEmacsRegularExpressionTests`: `PlusAndQuestionMarkAreUnescapedOperators`, `GroupingAlternationAndIntervalsRemainEscaped`, `DotMatchesNullButNotLineFeed`, `BackslashDoesNotEscapeClosingBracket` | Current Emacs provider behavior used by ptx. |
| Source/profile coupling found in A02/A03 | `IcodExpressionsA04CharacterizationTests`: `BasicProviderOverridesEmacsSyntaxRequestedByTac`, `PatternNewlineRemainsLiteralInCurrentBasicAndExtendedProviders`, `MidBranchCaretAndDollarRemainLiterals`, `EmptyAnchorBranchCurrentlyParticipatesAsAnEmptyCapture` | The Basic-provider override at the tac call shape, lack of pattern-newline alternation, contextual anchors, and an anchor-branch capture that agrees with running pinned Gnulib despite the latter's tolerated source-table expectation. The tac bugfix is tracked in [CoreUtils #190](https://github.com/uniblab/Icod.CoreUtils/issues/190). |
| Whole match, captures, backreferences | `GnuBasicRegularExpressionTests`: `SearchSelectsTheLongestMatchAtTheLeftmostPosition`, `OverallLongestMatchConstrainsSubexpressionSelection`, `EqualLengthMatchesFollowGnuGreedyCaptureOrdering`, `EmptyAndNonparticipatingCapturesRemainDistinct`; `GnuExtendedRegularExpressionTests`: `CapturesAndBackReferencesUseExtendedGrouping` | Internal GNU leftmost-longest and capture choices; ERE backreference acceptance is a GNU fixture, not a strict ERE guarantee. |
| Anchors, dot, NUL, line separator | `GnuBasicRegularExpressionTests`: `AnchorsHonorNewLineSensitivePolicy`, `DotMatchesNewlineButNeverNullByDefault`, `DotAndNegatedBracketExcludeNewlineWhenRequested`; `LineEditorRegularExpressionContractTests`: `ConsumerSelectedLineSeparatorControlsMultilineAndDotPolicy` | Current managed line policy, including Sed's NUL separator choice. |
| Brackets and locale | `GnuBasicRegularExpressionTests`: `PosixClassesRangesAndEquivalenceExpressionsAreSupported`, `EmptyRangeCompatibilityCanBeEnabledForExpr`, `CharacterClassesAreProviderDrivenAndInjectable`; `GnuExtendedRegularExpressionTests`: `CharacterClassesUseTheInjectedLocaleProvider` | Selected scalar classes, ranges, equivalence behavior and provider injection, not multicharacter collation completeness. |
| String, byte and UTF-8 coordinates | `LineEditorRegularExpressionContractTests`: `StringAndUtf8ByteSurfacesReturnAuthoritativeCoordinates`, `Utf8StartOffsetCannotSplitASourceScalar`, `MalformedUtf8CanRemainAnExactAuthoritativeByte`; `GnuExtendedRegularExpressionTests`: `ByteModeUsesOneMatchingUnitPerSourceByte`, `InvalidUtf8PolicyIsExplicitAndBytePreserving` | UTF-16 string indices, authoritative byte slices/offsets, and controlled split-scalar failures. |
| Prepared input and optimization parity | `PreparedRegularExpressionTests`, `PreparedRegularExpressionProfileTests`, `PublicPreparedByteInputTests`, `PublicPreparedByteInputIsolationTests`, `LiteralPrefixRegularExpressionTests`, and `R2_4RegularExpressionClosureTests` | Prepared/ordinary byte parity, source ownership, concurrency, malformed input, prefix fallback, resource accounting, and cancellation in selected cases. |
| Diagnostics and limits | `GnuBasicRegularExpressionTests`: `InvalidPatternsProduceStableDiagnostics`, `ConfiguredNestingLimitProducesAControlledDiagnostic`, `ConfiguredMatchStateLimitProducesAControlledFailure`, `CompileAndMatchHonorCancellation`; `GnuExtendedRegularExpressionTests`: `InvalidExtendedPatternsProduceStableDiagnostics` | Existing controlled result and cancellation paths. |
| Consumer-shaped supplemental cases | `IcodExpressionsA04ConsumerShapeTests`: prepared UTF-8 Grep offsets and captures, ProcPs anchored offset, Ptx bounded prefix, Sed NUL-line second search, Ed caller advance | Engine behavior at these call shapes; command invocation/output and consumer-owned iteration remain outside this assembly. The five cases are not part of the original 193-case checkpoint. |

## Required closure cases still open

| Priority | Gap | Next executable evidence |
|---|---|---|
| A01 | Strict BRE and strict ERE are not independently selectable APIs. Some malformed or escaped constructs are undefined or GNU-defined rather than standard-required. | Write separate draft `POSIX-BRE-CONTRACT.md` and `POSIX-ERE-CONTRACT.md` with section-level Issue 8 references; record standard status per fixture. Compare only defined cases to a pinned POSIX oracle and mark undefined cases without forcing one result. |
| A01 | Subexpression tie ordering, BRE backreference edge cases, and locale-sensitive bracket behavior need independent comparison. Multi-scalar collating elements are not representable in the current bracket model. | Add a differential table with pattern, input, locale, expected capture spans, oracle identity, and disposition; keep controlled `UnsupportedCollatingElement` as an inherited behavior fixture. |
| A02 | The current parser offers coupled Basic/Extended/Emacs modes, not switches for all 26 pinned Gnulib bits or 13 named macros. | For each implemented/coupled bit, test both polarities where meaningful; for missing bits, record a fixture as unsupported rather than pretending a profile is present. Compare named configurations with pinned Gnulib `regex.h`/engine. |
| A03 | Five additional consumer-shaped engine tests now cover selected prepared/anchored/repeated paths, but command-level invocation and output remain outside the focused CommandFramework test assembly. | Run source-pinned consumer tests for grep byte/UTF-8 offsets and empty matches, expr anchored capture return, ptx bounded search, Sed `-z`/`M` and replacement, Ed global substitution, diff line suppression, and procps offsets. Preserve tac's current result for extraction; fix it later under CoreUtils #190. |
| A04 | Nine C-locale ASCII cases compare against host glibc; an included pinned Gnulib runtime probe resolves four anchor-capture rows. No broad strict POSIX/GNU profile corpus is wired into the test suite. The original 110 declarations are local assertions, not 110 standards fixtures. | Extend the oracle matrix across public branches, locale and malformed input. Record executable versions, locale, encoding, invocation, normalized captures, and discrepancy classification. |
| A05 | The source SHA and first copy-time corpus are pinned, and a mechanical copy has occurred, but formal A05 acceptance is open while A01–A04 remain incomplete. | Finish the audit and characterize or explicitly classify each public/consumer gap. Identify any corpus additions without rewriting the first baseline; then freeze the final source/test identities, API, deviations, and CI run. Keep CommandFramework production unchanged. |

## Reproduction

```sh
rg -o '\[(Fact|Theory)\]' tests/CommandFramework.Tests/src/RegularExpressions -g '*.cs' | wc -l
dotnet test tests/CommandFramework.Tests/Icod.CommandFramework.Tests.csproj \
  --filter FullyQualifiedName~IcodExpressionsA04CharacterizationTests
```

The complete CommandFramework suite is exercised by PR CI on Linux, macOS,
and Windows. The independent oracle and full consumer-level evidence above
remain explicit gates before formal A05 acceptance.
