# Icod.Expressions A04: pinned Gnulib capture comparison

**Status:** Source-backed differential observations, not a run of the pinned
Gnulib engine or a POSIX conformance claim. The upstream identity is Gnulib
commit [`fb7312fa8d3df29f0ca0678f669b9a5b88a078ec`](https://github.com/coreutils/gnulib/tree/fb7312fa8d3df29f0ca0678f669b9a5b88a078ec),
[`tests/test-regex.c` lines 58–100](https://github.com/coreutils/gnulib/blob/fb7312fa8d3df29f0ca0678f669b9a5b88a078ec/tests/test-regex.c#L58-L100),
Git blob `0ab97819d2cdf3b29f5f4b989d1c33e0bd157f36`. That test source is
GPL-3.0-or-later and says its initial test table was copied from glibc
`posix/bug-regex11.c`. Its expected `regmatch_t` spans provide a precise
source reference, including nonparticipating groups represented by `-1,-1`.
No upstream test source has been copied into CommandFramework.

The managed observations below were produced in scratch with .NET SDK
10.0.402, `GnuExtendedRegularExpressionProvider` and
`PosixCLocaleRegularExpressionCharacterClassProvider.Instance`, on the pinned
CommandFramework production source `e1e118f18246eed290bee4758325a20e302bfb92`.
All input is ASCII, so the reported UTF-16 offsets coincide with byte
offsets. Each row gives whole-match `[start,end)` and then capture spans in
opening-parenthesis order. The upstream rows use `REG_EXTENDED`.

| Gnulib source line; pattern / input | Source-expected spans | Current managed spans | Classification |
|---|---|---|---|
| 82; <code>(^&#124;foo)bar</code> / `bar` | `(0,3); (-1,-1)` | `(0,3); (0,0)` | **Unresolved capture disagreement.** Both choose the same whole match; the current engine marks the anchor branch as a participating empty capture. |
| 83; <code>(foo&#124;^)bar</code> / `bar` | `(0,3); (-1,-1)` | `(0,3); (0,0)` | Same unresolved capture disagreement in the reversed alternation. |
| 89; `()x\1` / `x` | `(0,1); (0,0)` | `(0,1); (0,0)` | Agrees in this empty-backreference case. |
| 95; `a(b)()c\1` / `abcb` | `(0,4); (1,2); (2,2)` | `(0,4); (1,2); (2,2)` | Agrees on participating empty and nonempty captures. |
| 92; `([0-9]).*\1(a*)` / `7;7a` | `(0,4); (0,1); (3,4)` | `(0,4); (0,1); (3,4)` | Agrees on these repeated/backreference spans. |

The additive A04 theory
`EmptyAnchorBranchCurrentlyParticipatesAsAnEmptyCapture` uses two
independently written analogous patterns, with the anchor branch first and
last. Both rows pin the inherited behavior without copying an upstream
fixture into the test assembly. The exact
upstream examples were evaluated only in the scratch comparison. The
initial upstream-style assertion failed locally, then the characterization
assertion passed after recording the current result.

Do not change CommandFramework to force agreement during Stage A or the
mechanical extraction. Reproduce these source-pinned cases with a built
Gnulib oracle and adjudicate the capture rule before strict POSIX/GNU
normalization. This discrepancy is **unresolved**, not silently accepted as
a managed contract for the eventual normalized implementation.
