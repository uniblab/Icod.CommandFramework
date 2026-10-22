# Icod.Expressions A00: in-situ source and API inventory

**Status:** Working A00 inventory; not an A05 extraction freeze.  This document
records the existing engine before any production source is moved or refactored.

## Source identity and scope

- Repository: `uniblab/Icod.CommandFramework`
- Candidate analysis snapshot: `e1e118f18246eed290bee4758325a20e302bfb92`
- Package at that snapshot: `Icod.CommandFramework` 2.2.1, `net10.0`, C# 13,
  `AnyCPU`, `LGPL-3.0-or-later` in the project metadata.
- Existing namespace: `Icod.CommandFramework.RegularExpressions`.
- Production census: 32 C# files and one README in `src/RegularExpressions`.
- Focused test census: 10 C# files and one README in
  `tests/CommandFramework.Tests/src/RegularExpressions`; the C# files contain
  106 `[Fact]` or `[Theory]` declarations at the candidate snapshot.
- The new `uniblab/Icod.Expressions` repository has an initial editor
  configuration and LGPL-3.0 license text, but no extracted production code.

The Git blob manifest below accounts for **every tracked file** in the two
directories. Reproduce it with:

```sh
git ls-tree -r e1e118f18246eed290bee4758325a20e302bfb92 -- \
  src/RegularExpressions tests/CommandFramework.Tests/src/RegularExpressions
```

The commit, not a moving `main` branch or package version, is the source
authority for this inventory. A05 must decide whether to retain this commit as
the final extraction baseline after A01-A04 characterization changes.

## Non-destructive migration rule

`Icod.CommandFramework` retains its existing regular-expression implementation,
public API, and consumer behavior while `Icod.Expressions` is developed. The
first implementation in the new repository must **copy** the audited source and
tests against a frozen baseline; it must not remove them from CommandFramework,
replace its engine, or redirect its dependencies as part of that step.

Before any later CommandFramework removal or dependency switch is proposed,
the new library must build and pass the frozen semantic corpus and exact
extraction-parity tests, and each affected consumer must have a validated
migration path. Such a change belongs in a separate, reviewable PR. A00-A05
analysis and a successful source copy alone do not authorize it.

## Public source API at the candidate snapshot

All 24 top-level public types below are declared in the pinned source tree.
This is a **source declaration inventory**: the exact signatures, defaults,
enum values, nullability, record-generated members, and XML comments are those
in the linked commit. A compiled assembly/API export is still required before
the A00 acceptance gate can be closed; no SDK is available in the current
analysis environment.

| Surface | Public types and contract-bearing members |
|---|---|
| Compile | `IRegularExpressionProvider` (`Compile`, `CompileAsync`); `GnuBasicRegularExpressionProvider`, `GnuExtendedRegularExpressionProvider`, `GnuEmacsRegularExpressionProvider` (default and injectable-character-class constructors, `Default`, compile methods); `RegularExpressionCompileResult` (`IsSuccess`, `Expression`, `Diagnostic`, `Succeeded`, `Failed`) |
| Syntax/options | `GnuRegularExpressionSyntax` (`Basic = 0`, `Emacs = 1`, `Extended = 2`); `RegularExpressionOptions` (`Syntax`, `IgnoreCase`, `NewLineSensitive`, `LineSeparator`, `DotMatchesNull`, `AllowInvalidRepetitionOperators`, `AllowEmptyRanges`, `MaximumNestingDepth`, `MaximumMatchStates`, plus `GnuExprCompatibility`, `GnuExtendedCompatibility`, `GnuEmacsCompatibility`) |
| Match | `ICompiledRegularExpression` (`Pattern`, `CaptureCount`, string and byte `Match`/`MatchAsync` overloads); `RegularExpressionMatchOptions` (`StartIndex`, `RequireMatchAtStart`); `RegularExpressionByteMatchOptions` (`StartByteOffset`, `RequireMatchAtStart`); `RegularExpressionInputOptions` (`DecodingMode`, `InvalidEncodingPolicy`) |
| Prepared byte input | `RegularExpressionPreparedByteInput` (`Length`, `InputOptions`, `Prepare`); `CompiledRegularExpressionPreparedInputExtensions` (`Match`, `MatchAsync` over prepared input) |
| Outcomes | `RegularExpressionMatchResult`, `RegularExpressionByteMatchResult` (`IsSuccess`, `IsMatch`, `Match`, `Diagnostic`, `Succeeded`, `Failed`); `RegularExpressionMatch` (`Index`, `Length`, `Value`, `Captures`); `RegularExpressionByteMatch` (`ByteIndex`, `ByteLength`, `Value`, `Captures`) |
| Captures/diagnostics | `RegularExpressionCapture` (`Success`, `Index`, `Length`, `Value`); `RegularExpressionByteCapture` (`Success`, `ByteIndex`, `ByteLength`, `Value`); `RegularExpressionDiagnostic` (`Code`, `Message`, `PatternIndex`); `RegularExpressionDiagnosticCode` (14 named codes at this snapshot) |
| Locale policy | `IRegularExpressionCharacterClassProvider` (six classification/comparison methods); `UnicodeRegularExpressionCharacterClassProvider` (`Culture`, `CurrentCulture`, `InvariantCulture`, injectable culture); `PosixCLocaleRegularExpressionCharacterClassProvider` (`Instance`) |

The entry-point signatures are `Compile(string, RegularExpressionOptions?,
CancellationToken)`, string `Match(string, RegularExpressionMatchOptions?,
CancellationToken)`, byte `Match(ReadOnlyMemory<byte>,
RegularExpressionInputOptions?, RegularExpressionByteMatchOptions?,
CancellationToken)`, and `RegularExpressionPreparedByteInput.Prepare(
ReadOnlyMemory<byte>, RegularExpressionInputOptions?, CancellationToken)`;
each has the documented async counterpart where applicable. The byte result
uses authoritative source-byte offsets; string results use UTF-16 indices.
`ICompiledRegularExpression` explicitly promises GNU/POSIX leftmost-longest
selection. Do not generalize that interface into a PCRE contract unchanged.

The public regex API also names two types **outside** the source directory:
`Icod.CommandFramework.Text.TextDecodingMode` (`Bytes`, `Utf8`) and
`Icod.CommandFramework.Text.InvalidEncodingPolicy` (`PreserveBytes`, `Replace`,
`Throw`). They are defined in `src/Text/TextDecodingMode.cs` and
`src/Text/InvalidEncodingPolicy.cs`. Extraction must account for these
type identities and for any text-unit implementation dependency; a source-only
move of `src/RegularExpressions` is not sufficient.

## Internal component map

| Role | Existing source |
|---|---|
| Grammar and syntax tree | `GnuBasicRegularExpressionParser.cs`, `GnuBasicRegularExpressionSyntax.cs`; the parser handles Basic, Extended, and Emacs options despite its historical name. |
| Match and coordinates | `GnuBasicCompiledRegularExpression.cs` defines the general matcher, capture state, input decoding/coordinate conversion, state comparison, and resource context. |
| Repeated byte searches | `PreparedRegexInput.cs`, `PreparedCompiledRegularExpression.cs`, `IPreparedCompiledRegularExpression.cs`, `RegularExpressionPreparedByteInput.cs`. |
| Literal optimization | `RequiredLiteralPrefixAnalyzer.cs`, `LiteralPrefixCompiledRegularExpression.cs`; finite match-state limits constrain optimization selection. |
| Locale implementations | `UnicodeRegularExpressionCharacterClassProvider.cs`, `PosixCLocaleRegularExpressionCharacterClassProvider.cs` behind `IRegularExpressionCharacterClassProvider.cs`. |
| Diagnostics/results | `RegularExpressionDiagnostic*.cs`, compile/match result, capture, and match files listed in the manifest. |

These are observations of the current source layout, not proposed extraction
boundaries. In particular, `PreparedRegexInput` and the optimized match paths
were added after the original architecture review and need characterization.

## Focused tests and adjacent evidence

| Test file | Existing focus |
|---|---|
| `GnuBasicRegularExpressionTests.cs` | BRE, captures, options, byte coordinates, and CoreUtils compatibility |
| `GnuExtendedRegularExpressionTests.cs` | ERE and byte mapping |
| `GnuEmacsRegularExpressionTests.cs` | Gnulib Emacs profile used by `ptx` |
| `LineEditorRegularExpressionContractTests.cs` | Sed/Ed consumer boundary |
| `LiteralPrefixRegularExpressionTests.cs` | Required-prefix optimization and fallback |
| `PreparedRegularExpressionTests.cs` | Prepared-input parity, ownership, concurrency, coordinates |
| `PreparedRegularExpressionProfileTests.cs` | Prepared BRE/ERE/Emacs and guardrails |
| `PublicPreparedByteInputTests.cs` | Public prepared-input behavior |
| `PublicPreparedByteInputIsolationTests.cs` | Public snapshot/result isolation |
| `R2_4RegularExpressionClosureTests.cs` | R2.4 closure cases |

Adjacent text-unit tests live under `tests/CommandFramework.Tests/src/Text`,
not the regex test directory. Two benchmark projects exist under
`benchmarks/RegularExpressions.Benchmarks` and
`benchmarks/PreparedRegularExpressions.Benchmarks`; they are performance
evidence, not semantic parity oracles. A04 will decide which consumer-shaped
and text-unit cases to add to the frozen corpus.

## Pinned upstream references already recorded

The source README points to the CoreUtils upstream version ledger. That ledger
identifies these references for the existing GNU/POSIX foundation:

| Authority | Immutable identity | Use in this audit |
|---|---|---|
| GNU Coreutils 9.11 | tag `v9.11`, commit `c01fd163a47468a8296fb369f5233853bb551bb6` | `expr`, `ptx`, and other CoreUtils consumers |
| Gnulib used by Coreutils 9.11 | commit `fb7312fa8d3df29f0ca0678f669b9a5b88a078ec` | GNU regex syntax/configuration oracle for A02 |
| GNU grep 3.12 | tag `v3.12`, commit `3f8c09ec197a2ced82855f9ecd2cbc83874379ab` | grep consumer behavior |
| POSIX.1-2024 Issue 8 | document `9799919799` | strict BRE/ERE standards audit in A01 |
| GNU sed 4.10 | tag `v4.10`, commit `89b7a2224d4faa9d8baf76094b1232ad1477ef3e` | LineEditor consumer |
| GNU ed 1.22.5 | archive `ed-1.22.5.tar.lz`, SHA-256 `56e107ddc2f29dad6690376c15bf9751509e1ee3b8241710e44edbe5c3a158cc` | LineEditor consumer |

Source of pins: [`Icod.CoreUtils-Upstream-Version-Ledger.md`](https://github.com/uniblab/Icod.CoreUtils/blob/main/Icod.CoreUtils-Upstream-Version-Ledger.md),
especially Completion Gates C1 and R1. The audit still needs to verify the
exact regex-relevant Gnulib source and profile definitions at the pinned
revision; a release name alone is insufficient for A02.

## Provenance and remaining A00 gates

- The CommandFramework project declares `LGPL-3.0-or-later`; the new repository
  currently contains LGPL-3.0 license text. Preserve the source and test
  provenance and decide the eventual package expression explicitly during
  extraction. Do not infer per-file authorship from the package metadata.
- No per-file SPDX/copyright headers were found by a text search of the
  focused C# source and tests. This is **not** proof that all implementation
  and fixtures are original; inspect Git history and any imported fixtures
  before closing the provenance ledger.
- Repository history shows the focused paths first appeared in commit
  `b2c53304aaa9fc0d047e97d169c13195bcac8afc` on 20 August 2026.
  Commits touching those paths through the candidate snapshot identify
  `uniblab` or `Timothy Bruce`, both with `uniblab@hotmail.com`, as author.
  This establishes repository attribution only; it does not establish
  whether a test case or implementation fragment was adapted from upstream.
- A compiled public API export and a clean baseline test run are pending.
  The current analysis host has no `dotnet` executable, so this document does
  not claim build or test verification.
- A01-A04 must distinguish strict POSIX behavior, Gnulib syntax bits,
  consumer-owned profiles, and observed deviations. They may add tests and
  documentation in situ, but production semantics remain unchanged until
  A05 is frozen and extraction parity can be measured.

**A00 closure condition:** reconcile the compiled API export with this source
inventory, finish the file/fixture provenance ledger, and confirm every
upstream semantic claim against its immutable reference. Only then designate
an extraction source SHA at A05. The source manifest follows.

## Complete tracked source/test manifest

Git blob IDs make individual files independently traceable within the candidate
commit. These are repository object IDs, not SHA-256 checksums.

| Path | Git blob ID |
|---|---|
| `src/RegularExpressions/CompiledRegularExpressionPreparedInputExtensions.cs` | `22d5d21b7532d600008a03a601982d8b19bf0fa2` |
| `src/RegularExpressions/GnuBasicCompiledRegularExpression.cs` | `5c1951795830e79720f73e09cabdbdb37cb27982` |
| `src/RegularExpressions/GnuBasicRegularExpressionParser.cs` | `f173cb76c75396df6f86342293ac737792918642` |
| `src/RegularExpressions/GnuBasicRegularExpressionProvider.cs` | `3a49ee3fcb9a147bf4d5413d83ff613b7aa55b80` |
| `src/RegularExpressions/GnuBasicRegularExpressionSyntax.cs` | `3a063843e0a84ef7cc37b7a85002ebd35ae6277c` |
| `src/RegularExpressions/GnuEmacsRegularExpressionProvider.cs` | `e33f564fc115f0e9670d8e515dc9216f507ebc83` |
| `src/RegularExpressions/GnuExtendedRegularExpressionProvider.cs` | `6f554ed901d4be3cdc64da4638368466d6ba3a3b` |
| `src/RegularExpressions/GnuRegularExpressionSyntax.cs` | `2b92d3abb578028f809f5c94aa1691180bbbc57e` |
| `src/RegularExpressions/ICompiledRegularExpression.cs` | `7502d09cb0220d4823a8054733a193d6e4a210c8` |
| `src/RegularExpressions/IPreparedCompiledRegularExpression.cs` | `465480434e4ff93d1226ad6fe484247ac465f719` |
| `src/RegularExpressions/IRegularExpressionCharacterClassProvider.cs` | `77f0548f93c9ff33cb7422cbed1850d6a8ebfa86` |
| `src/RegularExpressions/IRegularExpressionProvider.cs` | `aa166fe89734e96801cb2e735a4ce5158b747117` |
| `src/RegularExpressions/LiteralPrefixCompiledRegularExpression.cs` | `7b236cef14a6063551c05e1b65ec9ab33a2800b0` |
| `src/RegularExpressions/PosixCLocaleRegularExpressionCharacterClassProvider.cs` | `47b0d39b1defc9ad54c1bba98fd6a2995e2044ed` |
| `src/RegularExpressions/PreparedCompiledRegularExpression.cs` | `1cb059b57c5a359cf7c33dba01c13fe26e8fb211` |
| `src/RegularExpressions/PreparedRegexInput.cs` | `d46b0215a5ee2a0c0600f7e758885e2ef43c55ea` |
| `src/RegularExpressions/README.md` | `c35ecc2b96260caea97eb1e54f82c87f7def7f2e` |
| `src/RegularExpressions/RegularExpressionByteCapture.cs` | `bbbdf44db37a971ee80ccdd59781e450928ffe92` |
| `src/RegularExpressions/RegularExpressionByteMatch.cs` | `42656efbda852b340443a20720a320825df7678e` |
| `src/RegularExpressions/RegularExpressionByteMatchOptions.cs` | `887246ee6e451254bab5e49f23431539b848c9f8` |
| `src/RegularExpressions/RegularExpressionByteMatchResult.cs` | `22f10a9ec78faf0bc6710ffaad578714b716c34e` |
| `src/RegularExpressions/RegularExpressionCapture.cs` | `3c6c02358996781de95199e9880e73f8685dfefc` |
| `src/RegularExpressions/RegularExpressionCompileResult.cs` | `190f2f86c34f78f752fc43537ca001cc75d80e0e` |
| `src/RegularExpressions/RegularExpressionDiagnostic.cs` | `52a67294ea454d7d50cfca4611248016b114a577` |
| `src/RegularExpressions/RegularExpressionDiagnosticCode.cs` | `5fa8c1c570845b7d13e52d78f442e54e9491cc88` |
| `src/RegularExpressions/RegularExpressionInputOptions.cs` | `1ebbc9acdf6b7a478df202c190f9ecea4fb8c9c7` |
| `src/RegularExpressions/RegularExpressionMatch.cs` | `9d3b4aa4e6f4b5016ce8e2e4be5ea27ba922894c` |
| `src/RegularExpressions/RegularExpressionMatchOptions.cs` | `7d55fff95e1d780a1e2ee3d0244ff8839ac75055` |
| `src/RegularExpressions/RegularExpressionMatchResult.cs` | `7527ee3e709f985807e98a719e7a4cae7cdcc893` |
| `src/RegularExpressions/RegularExpressionOptions.cs` | `d2eb21c84eae1b7fce2079c8b76a238cbfe146ae` |
| `src/RegularExpressions/RegularExpressionPreparedByteInput.cs` | `c1e4ac0fae67e64054eb24d9051374a1baa936ab` |
| `src/RegularExpressions/RequiredLiteralPrefixAnalyzer.cs` | `e517d9abb49d329c9470095d2dfbb3aabdeff762` |
| `src/RegularExpressions/UnicodeRegularExpressionCharacterClassProvider.cs` | `f797a5b7790ef1bf33287d8a4f74ae87bfd2a407` |
| `tests/CommandFramework.Tests/src/RegularExpressions/GnuBasicRegularExpressionTests.cs` | `8d32f8728b8eb746c7e01e7c0e143c836481c629` |
| `tests/CommandFramework.Tests/src/RegularExpressions/GnuEmacsRegularExpressionTests.cs` | `f5a5182f86bfa7ef746419baf06d5e848c7d65a6` |
| `tests/CommandFramework.Tests/src/RegularExpressions/GnuExtendedRegularExpressionTests.cs` | `98be9ebda6dc3a251a2d7ba3fa9f26692cd085da` |
| `tests/CommandFramework.Tests/src/RegularExpressions/LineEditorRegularExpressionContractTests.cs` | `429ab21e8ca849c09524695031841394dc6bb14d` |
| `tests/CommandFramework.Tests/src/RegularExpressions/LiteralPrefixRegularExpressionTests.cs` | `0219c1b1f8d09aad126a4097147bd3a93157b607` |
| `tests/CommandFramework.Tests/src/RegularExpressions/PreparedRegularExpressionProfileTests.cs` | `f4bf0de57e6dfdc896dc13023b8e66b1dc7f2ae9` |
| `tests/CommandFramework.Tests/src/RegularExpressions/PreparedRegularExpressionTests.cs` | `a97f137c3048c319234250c40639df7086ac6b15` |
| `tests/CommandFramework.Tests/src/RegularExpressions/PublicPreparedByteInputIsolationTests.cs` | `44d9f850c233d7d53253de702e8b8be92c8b3eb1` |
| `tests/CommandFramework.Tests/src/RegularExpressions/PublicPreparedByteInputTests.cs` | `da24093369581bfbe40b4f655537b48ccbbb8159` |
| `tests/CommandFramework.Tests/src/RegularExpressions/R2_4RegularExpressionClosureTests.cs` | `a35ab805ca570cedae62149941e6f1b3650d8e81` |
| `tests/CommandFramework.Tests/src/RegularExpressions/README.md` | `54cc715b8da972321490916e0cf026d2f9d8403a` |
