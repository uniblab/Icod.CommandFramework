# Icod.Expressions A04: exploratory GNU grep probe

**Status:** Independent runtime comparison for selected whole-match cases,
not the pinned Gnulib oracle, a POSIX conformance report, or A04 closure.
The managed side is the 44-case [in-situ parity baseline](Icod.Expressions-A04-Parity-Baseline.jsonl)
from CommandFramework source commit `e1e118f18246eed290bee4758325a20e302bfb92`.

The comparison executable is `/usr/bin/grep`, reporting `grep (GNU grep)
3.11`, SHA-256
`20a35a4c18f2fe9e6305c1cbf866b9c0fcc3f957132ae66028c99ec353bb0a80`.
Commands ran on Linux with `LC_ALL=C` on 22 October 2026. `-b -o`
reports byte offsets and the selected nonempty match. GNU grep is an
application with record and pattern-list policy; these invocations do not
prove exact parity with the pinned Gnulib `lib/regex.h` revision
`fb7312fa8d3df29f0ca0678f669b9a5b88a078ec`. In particular, this
probe cannot compare captures, diagnostics, empty matches, or arbitrary
start offsets.

| Case / grep mode after `LC_ALL=C` | GNU grep 3.11 (exit; stdout) | Current engine fixture | Disposition |
|---|---|---|---|
| `bre-gnu-alternation` / `grep -G` | `0`; `0:aa` | `aa` at 0 | Agrees for this GNU BRE extension; no strict BRE inference. |
| ERE alternation / `grep -E` | `0`; `0:aa` | `ExtendedSelectionIsLeftmostLongest` selects `aa` at the first viable start in its separate input | Agrees on the longest alternative; this is not an identical-input differential fixture. |
| `ere-gnu-backreference` / `grep -E` | `0`; `0:abab` | `abab` at 0 | Agrees for this GNU ERE extension; no strict ERE inference. |
| `bre-mid-anchor` / `grep -G` | `0`; `0:a^b` | Treats middle `^` literally | Agrees for this Basic context. |
| Extended middle `^` / `grep -E` | `1`; empty | A04 Extended test treats middle `^` literally and matches `a^b` | **Unresolved profile difference.** The A02 matrix marks `RE_CONTEXT_INDEP_ANCHORS` missing; compare exact pinned Gnulib Extended syntax before classifying the target behavior. Preserve the inherited result during extraction. |
| `ere-invalid-duplicate` / `grep -E` | `0`; `0:aaaa` | Default Extended fails to compile; `gnu-extended-invalid-duplicate` matches `aaaa` | The current `GnuExtendedCompatibility` preset agrees for this case. The default provider is a different configuration; do not equate it with GNU grep's complete profile. |

The displayed `grep` outputs omit their terminating line feed. `grep -o`
cannot report capture spans, and its line-splitting or `-z` behavior belongs
partly to the application layer. The next differential pass needs an
executable built from a pinned Gnulib revision or a source-pinned utility,
plus captured compile/match diagnostics and subexpression registers.

Reproduce the identity and selected command forms:

```sh
grep --version | head -1
sha256sum /usr/bin/grep
printf 'aa\n' | LC_ALL=C grep -G -b -o 'a\|aa'
printf 'aa\n' | LC_ALL=C grep -E -b -o 'a|aa'
printf 'abab\n' | LC_ALL=C grep -E -b -o '(ab)\1'
printf 'a^b\n' | LC_ALL=C grep -G -b -o 'a^b'
printf 'a^b\n' | LC_ALL=C grep -E -b -o 'a^b'
printf 'aaaa\n' | LC_ALL=C grep -E -b -o 'a**'
```
