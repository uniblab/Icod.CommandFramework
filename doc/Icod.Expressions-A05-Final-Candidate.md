# Icod.Expressions A05: accepted extraction corpus

**Status:** Accepted on 2026-10-23 as the bounded extraction baseline with
the classified gaps below; not a strict POSIX/Gnulib conformance decision,
a stable release, or a CommandFramework migration. The
[first copy-time checkpoint](Icod.Expressions-A05-Extraction-Baseline.md)
remains immutable. This record adds the later fixtures and classifies what
the extraction contract can and cannot establish.

## Frozen candidate inputs

| Input | Identity and check |
|---|---|
| Production | CommandFramework commit `e1e118f18246eed290bee4758325a20e302bfb92`, regex tree `bfe11d5f5ff6a75d75de68573edea6125706b87e`, and two external Text enum blobs. The Expressions source copy retains all 35 source/README/enum blobs; `tools/verify-frozen-source.sh` checks each. |
| Public API | The original 24 regex types and two Text enums in `Icod.Expressions-A00-Compiled-API.txt`, SHA-256 `8e64436d919f0b9fba74de88a03c2154960fe33d8a29675d6b360c15786abc82`. The Expressions CI compares compiled public member lines across assemblies. The assembly identity intentionally differs. |
| Original tests and fixture | Eleven focused C# blobs, 193 runtime cases, and the 44-case JSONL (SHA-256 `61696259b079b0ef48b33326b51db8a37083e2dd9219825024398786c3cea4ff`) in the [first checkpoint](Icod.Expressions-A05-Extraction-Baseline.md#focused-test-git-blobs). `tools/verify-frozen-corpus.sh` retains their identities. |
| Additive tests | `IcodExpressionsA04ConsumerShapeTests.cs` blob `589a52b42d5e3010e9e678f771cc85b156236e0b` (five cases) and `IcodExpressionsA02SyntaxOptionCharacterizationTests.cs` blob `ef9cd9e358460f412d343c91db228c6179c964ff` (16 cases). Both blobs are identical in CommandFramework and Expressions. Total: **13 focused C# files, 214 runtime cases**. |
| Independent runtime comparisons | C-locale Linux/glibc 15-case differential runner blob `59a10c631112307159a0682a493062de4cd82c80` in both repositories; 13 agreement cases and two defined ERE anchor divergences. The Expressions pinned included-Gnulib runner blob `7511107d8c12d1e98b53c88311c2d44ae0c0c387` checks four anchor-capture observations. These are supplemental oracle evidence, not extra focused test cases. |
| Source-pinned consumer gate | The Expressions package-backed scratch bridge runs 725 original CoreUtils, ProcPs, DiffUtils, Grep, and LineEditor tests before and after substitution on Windows, macOS, and Linux: 611 regex-consumer cases plus 114 CoreUtils shared-Text cases, including one direct Shared Text API case. The bridge is not a distributable dependency switch. |

The Expressions [`verify-a05-candidate.sh`](https://github.com/uniblab/Icod.Expressions/blob/bootstrap/pre-extraction-foundation/tools/verify-a05-candidate.sh)
checks the original manifests, the 13-file census, both additive test blobs,
and both oracle-tool blobs. CI separately requires exactly 214 passing
focused cases and byte-identical 44-case JSONL on .NET 8, 9, and 10 across
Linux, Windows, and macOS. [Expressions CI run
40018021229](https://github.com/uniblab/Icod.Expressions/actions/runs/40018021229)
passed all five jobs with the consolidated verifier, source/API/fixture
guards, 15-case differential, included Gnulib oracle, alpha package smoke,
cross-platform old-client forwarding check, and the 725-case consumer gate
before and after scratch substitution on each operating system. Its
[final E02 review](https://github.com/uniblab/Icod.Expressions/blob/bootstrap/pre-extraction-foundation/doc/E02-Final-Parity-Review.md)
records the same-run comparison against the frozen CommandFramework audit
commit: 35 source blobs and 13 focused test blobs match, both .NET 10 suites
pass the same 214 test names/outcomes, and both runners verify the exact
44-case JSONL. The audit commit is retained by the
[audit ref](https://github.com/uniblab/Icod.CommandFramework/tree/audit/icod-expressions-a05-frozen). [CommandFramework CI run
39994624392](https://github.com/uniblab/Icod.CommandFramework/actions/runs/39994624392)
passed its full suite and parity checks on the corresponding candidate.
The executable A05 verification gate is satisfied. The user accepted the
bounded A05/E02 review and its classified limits on 2026-10-23. This freezes
the identities for extraction comparisons without closing the later strict
POSIX/GNU work or changing CommandFramework production.

## Classified limits of this extraction contract

| Gate | Present evidence | Classification and later owner |
|---|---|---|
| A00 provenance | The [source/test manifest](Icod.Expressions-A00-In-Situ-Inventory.md) traces pinned Git history and preserves LGPL metadata. | Attribution is retained; historical originality of every fixture is not claimed. Any licensing discrepancy needs a separate provenance correction. |
| A01 strict BRE/ERE | Draft [BRE](POSIX-BRE-CONTRACT.md) and [ERE](POSIX-ERE-CONTRACT.md) contracts, [semantic audit](Icod.Expressions-A01-POSIX-Semantic-Audit.md), and 15-case C-locale differential. | **Defined divergence:** inherited Extended matches middle `^`/`$` as literals where Issue 8 §9.4.9 anchors make those inputs unmatchable. Strict BRE/ERE are not separately selectable; multicharacter collation and non-C locale equivalence remain unproved. Strict surfaces require later implementation and differential evidence. |
| A02 GNU profiles | [All 26 bits and 13 macros](Icod.Expressions-A02-GNU-Syntax-Bit-Matrix.md) mapped; 16 additional managed option cases. | **Missing controls and profiles:** Basic/Extended/Emacs bundles cannot independently select `RE_CONTEXT_INDEP_ANCHORS`, `RE_NEWLINE_ALT`, `RE_NO_SUB`, and other classified bits. Exact named compositions and result policy belong to N01; current bundles must not be advertised as exact Gnulib profiles. |
| A03 consumers | [Call-site matrix](Icod.Expressions-A03-Consumer-Compatibility-Matrix.md) and 725 source-pinned tests through the scratch bridge, including 114 shared-Text cases. | `tac` provider/options bug is deferred to [CoreUtils #190](https://github.com/uniblab/Icod.CoreUtils/issues/190); direct `diff -I` correction to [DiffUtils #8](https://github.com/uniblab/Icod.DiffUtils/issues/8). Remaining callers, cross-platform distributable forwarding, and actual consumer migrations belong to E03 and separate consumer PRs. |
| A04 parity | [Characterization index](Icod.Expressions-A04-Characterization-Index.md), 214 focused tests, 44 JSONL cases, 15 glibc cases, and four pinned Gnulib capture observations. | **Bounded evidence:** broader locale, malformed-input, capture-tie, and GNU profile differentials are not established. The Gnulib table's absent-register expectation explicitly tolerates the observed participating `(0,0)` capture; it is not a runtime disagreement for the four probed rows. |

A05 acceptance freezes these identities and carries the classified limitations
into later strict/GNU work. It establishes **preservation of the inherited
engine for this bounded corpus**, not full standards conformance, exact Gnulib
profile behavior, a stable release, or authorization to alter CommandFramework
production. Publish Expressions, then migrate and validate every known
consumer before removing or replacing any CommandFramework production regex
code. A future ownership change needs a separate consumer-validated PR.
