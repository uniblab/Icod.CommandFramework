# Icod.Expressions A04: Linux POSIX regex differential

**Status:** executable, bounded comparison with an independent POSIX regex
implementation. It is not a POSIX.1-2024 conformance certificate; the
separate [pinned Gnulib runtime probe](Icod.Expressions-A04-Gnulib-Capture-Comparison.md)
addresses the anchor-capture source-table ambiguity. The standards reference is
[Issue 8, chapter 9](https://pubs.opengroup.org/onlinepubs/9799919799/basedefs/V1_chap09.html),
especially §§9.1, 9.3 and 9.4. Defined examples below use ASCII subject and
pattern text under the C locale; a byte offset therefore equals the managed
UTF-16 index for these particular cases.

The C#-only `tools/PosixRegexDifferential` invokes Linux/glibc `regcomp`,
`regexec`, and `regfree` through P/Invoke. It calls `setlocale(LC_ALL, "C")`,
uses BRE or `REG_EXTENDED` ERE compilation, and compares the whole match and
every expected capture register with the unchanged CommandFramework provider.
Its native interop assumes the default glibc `regmatch_t` layout (two 32-bit
offsets) and `REG_EXTENDED=1`; the project is deliberately Linux/glibc only.
No C oracle source or native dependency is shipped in either library.

The local reference host reports **Ubuntu glibc 2.39-0ubuntu8.6**, .NET SDK
10.0.402, and CommandFramework production source
`e1e118f18246eed290bee4758325a20e302bfb92`. The fixture runner source
is the same Git blob in both draft PRs:
`bec36b11aa2bd1617e191ac49703244a03dcc10b`. Its project reference
points to the respective assembly. Reproduce on Linux:

```sh
dotnet run --project tools/PosixRegexDifferential/PosixRegexDifferential.csproj -c Staging
```

| Case family | glibc versus inherited managed result | Classification |
|---|---|---|
| BRE interval longest match, backreference capture, empty capture | Agreed on all three exact register lists. | Defined ASCII/C-locale samples; agreement for these inputs only. |
| ERE longest alternative, subexpression tie, digit class, alternative before a suffix | Agreed on all four exact register lists. | Defined ASCII/C-locale samples; not a locale or full grammar certification. |
| ERE `(^\|foo)bar` and `(foo\|^)bar` on `bar` | Both return whole `(0,3)` and participating empty group `(0,0)`. | Independent glibc observation; the included pinned Gnulib engine also reports `(0,0)`. Its test table lists `(-1,-1)` but explicitly tolerates a different actual span. |

All nine current differential rows matched their independently recorded
glibc and managed expectations locally. The tool fails on any changed
register list, including a change that made both engines agree for a new
reason. The Linux CI job reruns it. The Gnulib runtime probe covers four
anchor captures; broader strict BRE/ERE coverage, non-C locales,
multicharacter collation, malformed input, and consumer command behavior
remain open A01/A04 work.
