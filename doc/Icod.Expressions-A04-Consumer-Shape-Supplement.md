# Icod.Expressions A04: consumer-shaped supplemental cases

**Status:** additive in-situ characterization after the first copy-time
checkpoint. No CommandFramework production file or consumer call site changed.
The original 193 focused cases and 44-case JSONL retain their identities in
the [candidate A05 checkpoint](Icod.Expressions-A05-Extraction-Baseline.md).

`tests/CommandFramework.Tests/src/RegularExpressions/IcodExpressionsA04ConsumerShapeTests.cs`
is five new `[Fact]` declarations and five runtime cases. Its Git blob is
`589a52b42d5e3010e9e678f771cc85b156236e0b`. The same blob is copied to
`Icod.Expressions/tests/Icod.Expressions.Tests/RegularExpressions` without
rewriting outcomes. The exact tests and source identity belong in the final
A05 corpus after the remaining audit gates close.

| Case | Inherited engine contract | Consumer policy outside this test |
|---|---|---|
| Grep prepared UTF-8 repeated search | The prepared source preserves byte offsets and a capture across calls starting at bytes 2 and 4. | Grep chooses its record, next start, and output format. |
| ProcPs anchored offset | An unanchored string search finds `dog` at UTF-16 index 2; anchoring at 0 rejects it, anchoring at 2 finds it. | ProcPs maps matches to process selection and errors. |
| Ptx bounded prefix | Emacs `a+` matches two characters in the supplied prefix and three in the full input. | Ptx constructs the prefix and advances across empty matches. |
| Sed NUL line separator | A second call beginning after the first `^b$` match finds the later NUL-delimited `b`. | Sed owns repeated substitution and replacement. |
| Ed caller advance | `a*` first returns an empty match at 0; calling again from index 1 returns `a`. | Ed owns empty-match advancement and occurrence selection. |

Local Staging .NET 10 verification after adding this file: 536/536 full
CommandFramework tests and 198/198 focused Expressions tests passed; the
five-case filtered run in CommandFramework also passed. The
[CommandFramework CI run 39872377165](https://github.com/uniblab/Icod.CommandFramework/actions/runs/39872377165)
and [Expressions CI run 39872443907](https://github.com/uniblab/Icod.Expressions/actions/runs/39872443907)
completed successfully on their corresponding PR heads. Expressions CI
requires exactly 198 focused tests per target framework on Windows, Linux,
and macOS. These tests do not replace command-level consumer suites, strict
POSIX fixtures, or the pinned Gnulib differential oracle.
