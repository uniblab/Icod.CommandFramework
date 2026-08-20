# Icod.CommandFramework.Text

This namespace contains reusable text-unit, display-column, tab-stop, and byte-preserving logical-line infrastructure for command-line applications.
## Design rules
- Decode input only to make classification and width decisions; preserve the exact source bytes for reproduction.
- Treat a UTF-8 byte-order mark as ordinary input rather than metadata.
- Make malformed-input handling explicit: preserve each byte, replace while retaining the source byte, or throw at a stable byte offset.
- Keep locale blank classification and display-width calculation independently injectable.
- Resolve the process profile through `LC_ALL`, `LC_CTYPE`, then `LANG`, treating only `C` and `POSIX` as raw-byte locales.
- Use checked `ulong` display columns and recurring tab-stop arithmetic.
- Expose the maximum configured tab-stop distance so consumers can bound pending storage.
- Share mechanisms, not command semantics. Command projects retain ownership of option precedence, parsing syntax, formatting policy, buffering policy, file-boundary behavior, and output diagnostics.
- Use `TextLineReader` when a consumer needs logical lines without surrendering exact bytes or treating a byte-order mark as metadata.
## Portability profile
The initial implementation supports exact raw-byte iteration for the POSIX C locale and exact byte-preserving UTF-8 scalar iteration. Its deterministic Unicode blank profile recognizes horizontal breakable space separators and excludes U+00A0, U+2007, and U+202F; callers may inject another locale policy. It does not claim transparent compatibility with arbitrary stateful legacy encodings.
The managed Unicode display-width provider is deterministic across operating systems, uses Unicode 16.0.0 East Asian Width data, assigns ambiguous-width scalars one column, and measures Unicode scalars rather than grapheme clusters.
## Tab-stop model

`TabStopSet` represents explicit zero-based tab stops together with an optional recurring continuation.
- `TabStopSet.Every(N)` creates globally aligned stops every `N` columns.
- `TabStopSet.Create` accepts a strictly increasing explicit stop list plus an optional continuation.
- `TabStopContinuation.Absolute(N)` continues at global multiples of `N`.
- `TabStopContinuation.Relative(N)` continues at offsets of `N` from the final explicit stop, or from column zero when no explicit stop exists.
- An explicit list without continuation is exhausted after its final stop.

Command-specific tab-list syntax and diagnostics are intentionally outside this package.
## Logical lines

`TextLineReader` groups text units at an exact line-feed byte. The line feed is represented by `TextLine.HasLineFeed`; it is not included in `TextLine.Units`. A carriage return therefore remains part of the content. `TextLine.ToByteArray` and `TextLine.WriteAsync` reproduce retained bytes, while `ToDecodedString` supplies a non-authoritative matching surface for managed consumers.
