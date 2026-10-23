# Icod.Expressions A04: pinned Gnulib capture comparison

**Status:** Bounded C-locale runtime comparison, not a POSIX conformance
claim. The upstream identity is Gnulib commit
[`fb7312fa8d3df29f0ca0678f669b9a5b88a078ec`](https://github.com/coreutils/gnulib/tree/fb7312fa8d3df29f0ca0678f669b9a5b88a078ec),
[`tests/test-regex.c`](https://github.com/coreutils/gnulib/blob/fb7312fa8d3df29f0ca0678f669b9a5b88a078ec/tests/test-regex.c),
Git blob `0ab97819d2cdf3b29f5f4b989d1c33e0bd157f36`. That test source is
GPL-3.0-or-later and says its initial table came from glibc
`posix/bug-regex11.c`. No upstream C source is committed in either Icod
repository.

The upstream table *lists* an absent group `(-1,-1)` for two BRE and two ERE
anchor branches. Its comparison loop intentionally **does not fail** on a
different actual span whenever the expected register is `(-1,-1)`; see
[`tests/test-regex.c` lines 148–159](https://github.com/coreutils/gnulib/blob/fb7312fa8d3df29f0ca0678f669b9a5b88a078ec/tests/test-regex.c#L148-L159).
Consequently, those table entries are tolerated examples, not proven runtime
registers. Earlier A04 notes treated them as a disagreement with Gnulib;
the executable check corrects that interpretation.

The [Expressions oracle runner](https://github.com/uniblab/Icod.Expressions/blob/bootstrap/pre-extraction-foundation/tools/GnulibRegexOracle/run.sh)
checks out that exact commit, verifies the test blob, generates a temporary
`regex-tests` project, configures `--with-included-regex`, and checks that
Gnulib's `regex.o` symbol `rpl_regcomp` is defined in the linked test binary.
It first runs the unmodified upstream test. It then removes only the
two-line tolerance from the **temporary** test copy, rebuilds, and requires
exactly four reported `rm[1] 0..0` results for upstream rows 5–8. The
[diagnostic CI run](https://github.com/uniblab/Icod.Expressions/actions/runs/39892305953)
showed all four results with the included engine. The maintained runner
checks that observation on the PR's Linux Staging job.

| Pattern / input | Upstream table | Included Gnulib runtime | glibc 2.39 / managed runtime | Interpretation |
|---|---|---|---|---|
| BRE `\(^\|foo\)bar` / `bar` | `(0,3); (-1,-1)` | `(0,3); (0,0)` | Not in 15-case glibc runner / not compared here | GNU BRE anchor capture participates; the source table tolerates it. |
| BRE `\(foo\|^\)bar` / `bar` | `(0,3); (-1,-1)` | `(0,3); (0,0)` | Not in 15-case glibc runner / not compared here | Same observation with reversed branch order. |
| ERE <code>(^&#124;foo)bar</code> / `bar` | `(0,3); (-1,-1)` | `(0,3); (0,0)` | `(0,3); (0,0)` | All three running engines agree for this input. |
| ERE <code>(foo&#124;^)bar</code> / `bar` | `(0,3); (-1,-1)` | `(0,3); (0,0)` | `(0,3); (0,0)` | Same agreement with reversed branch order. |

The managed values use the unchanged CommandFramework production source
`e1e118f18246eed290bee4758325a20e302bfb92`, the Extended provider,
and the C-locale character-class provider. The glibc observations use the
[15-case C# differential](Icod.Expressions-A04-POSIX-Glibc-Differential.md).
ASCII inputs make byte and UTF-16 offsets equal here. The additive test
`EmptyAnchorBranchCurrentlyParticipatesAsAnEmptyCapture` pins analogous
managed cases without importing the upstream source.

This settles the **apparent source-table disagreement for these two ERE
cases**. It does not determine the strict POSIX rule for all capture ties,
prove locale behavior, or establish exact parity for every Gnulib syntax
profile. Preserve CommandFramework's implementation during extraction.
