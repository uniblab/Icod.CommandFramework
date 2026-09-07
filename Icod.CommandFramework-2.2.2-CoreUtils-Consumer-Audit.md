# Icod.CommandFramework 2.2.2 — CoreUtils Consumer Audit

## Scope

This audit examines six `Icod.CoreUtils` consumers against the public regular-expression and text infrastructure available in `Icod.CommandFramework` 2.2.1/2.2.2:

- `ptx`
- `csplit`
- `tac`
- `nl`
- `expr`
- `numfmt`

The specific question is whether these commands should rely more directly on `Icod.CommandFramework`, especially the immutable `RegularExpressionPreparedByteInput` surface introduced in 2.2.0 and optimized in 2.2.1.

The audit deliberately separates three concerns:

1. **semantic ownership** — whether GNU/POSIX-visible behavior belongs on the framework regular-expression engine rather than `System.Text.RegularExpressions`;
2. **prepared-input reuse** — whether the same authoritative byte record is searched repeatedly enough to amortize preparation;
3. **command policy** — behavior that should remain in `Icod.CoreUtils` even when framework mechanism is used.

## Executive result

| Command | Current mechanism | Prepared-input opportunity | Recommendation |
| --- | --- | --- | --- |
| `ptx` | GNU Emacs engine, but repeatedly converts authoritative bytes to Latin-1 strings | **High** | Migrate custom word/sentence matching to prepared byte input with `TextDecodingMode.Bytes`; validate GNU behavior first |
| `tac` | GNU basic engine over byte windows; repeated searches advance through the same window | **High** | Prepare each search window once and reuse it while enumerating matches |
| `csplit` | GNU regex engine; typically one match attempt per line per active scan | **Low to medium** | Keep ordinary byte `Match` initially; benchmark before adding preparation/cache complexity |
| `nl` | GNU regex engine over already-decoded logical lines; one match per line | **Low** | Keep string matching; prepared byte input is not justified by current call shape |
| `expr` | GNU Expr compatibility profile; one anchored match per operator evaluation | **None/low** | Keep current framework string match; no prepared-input migration |
| `numfmt` | source-generated .NET regex parses the command's own `%f` format mini-language | **None** | Keep `GeneratedRegex`; this is not GNU/POSIX regular-expression semantics |

No new public framework API is required for these consumers. The 2.2.x prepared-input surface is sufficient for the two strongest opportunities, `ptx` and `tac`.

## 1. `ptx`

### Current shape

`ptx/src/PtxPatterns.cs` already compiles its custom word and sentence expressions through `GnuEmacsRegularExpressionProvider`. However, the matching path converts authoritative bytes to Latin-1 strings and then repeatedly calls the string `Match` API:

- `FindWords` converts the complete context with `Latin1.GetString(...)` and repeatedly searches it while advancing `StartIndex`;
- `FindSentenceSeparator` searches a predecoded one-to-one string;
- `SkipSomething` repeatedly converts `context[..limit]` to Latin-1 before an anchored match.

The one-byte-to-one-UTF-16-code-unit Latin-1 conversion is an implementation bridge, not command policy.

### Why 2.2.x is a better fit

`RegularExpressionPreparedByteInput` exists specifically for one authoritative byte record searched repeatedly. `ptx` has exactly that shape.

For the current one-byte-per-unit semantics, preparation should use:

```text
RegularExpressionInputOptions {
    DecodingMode = TextDecodingMode.Bytes
}
```

That preserves the existing one-byte-to-one-match-unit intent without allocating a Latin-1 string merely to obtain stable indices.

### Recommended migration

- Prepare each effective context once.
- Reuse the prepared input while enumerating word matches.
- Reuse the prepared complete input/context for repeated sentence-separator searches where lifetime permits.
- Replace `DecodeForRegularExpression` and internal Latin-1 matching bridges only after differential tests prove identical GNU-visible behavior.
- Preserve `PtxWordSpan` byte offsets and existing zero-length-match safeguards.

### Risk

The principal risk is semantic rather than API-related. The current Latin-1 bridge hard-codes one-byte units. The migration must therefore explicitly select `TextDecodingMode.Bytes`; silently accepting the framework default UTF-8 mode would be a behavior change for bytes >= 0x80.

**Verdict: high-confidence consumer migration; no framework API addition needed.**

## 2. `tac`

### Current shape

Regex mode compiles through the framework GNU regular-expression engine and searches byte windows. The reverse-index algorithm repeatedly invokes `expression.Match(...)` against a window while changing `StartByteOffset` to enumerate matches and locate the previous separator.

The ordinary public byte `Match` surface prepares input for each call. Re-searching the same window therefore repeats preparation work.

### Recommended migration

At the window boundary inside the regex reverse-search logic:

1. read/build the window bytes exactly as today;
2. construct one `RegularExpressionPreparedByteInput` with the same `RegularExpressionInputOptions` currently used by `tac`;
3. enumerate all required matches against that prepared object while advancing `RegularExpressionByteMatchOptions.StartByteOffset`;
4. discard the prepared object when the window changes.

The migration should not change:

- window growth/overlap policy;
- separator-before/after semantics;
- byte-coordinate calculations;
- zero-length separator handling;
- input spooling or reverse-index policy.

This is almost exactly the repeated-search workload used to justify and benchmark the 2.2.0 prepared-input API.

**Verdict: high-confidence consumer migration; likely the cleanest real CoreUtils validation case after grep.**

## 3. `csplit`

### Current shape

`csplit` indexes the input by line, reads one candidate line into a byte array, and applies the active regex control. A scan advances line by line until the first match.

Although the command may perform many regex calls overall, each candidate line is normally matched once before being discarded. Preparation would therefore add an immutable snapshot/preparation step without obvious reuse.

Repeated controls can cause later scans to revisit lines, but the current architecture rereads those lines from the spool rather than retaining a stable record object across controls.

### Recommendation

Do **not** migrate mechanically to `RegularExpressionPreparedByteInput`.

First benchmark realistic workloads:

- long files with no match until near EOF;
- many short lines;
- large individual lines;
- repeated regex controls revisiting overlapping ranges.

A useful optimization would require either demonstrated benefit from prepared single-match input or a bounded cache keyed by line identity. The latter adds memory and invalidation complexity and should not be introduced without measurements.

The current use of the framework byte regex API is architecturally correct.

**Verdict: framework dependency is correct; prepared input is not yet justified.**

## 4. `nl`

### Current shape

Pattern numbering styles compile through `Icod.CommandFramework.RegularExpressions`. Each logical `TextLine` is decoded and matched once to decide whether the line receives a number.

### Analysis

This is not the repeated-byte-search shape targeted by prepared input:

- input is already represented as framework `TextLine`/`TextUnit` data;
- each line normally receives one regex decision;
- the command needs a Boolean match result, not repeated enumeration of captures/positions over the same line.

Converting each line back into an authoritative byte snapshot merely to prepare it would likely increase work.

A future optimization could avoid transient decoded-string allocation if the text subsystem eventually offers a direct regex bridge over `TextLine`, but that would be a separate API/design question and is not warranted by the 2.2.x prepared-byte contract alone.

**Verdict: keep current string match. No 2.2.x migration required.**

## 5. `expr`

### Current shape

`expr` correctly uses the framework's dedicated GNU Expr compatibility regular-expression profile and `RequireMatchAtStart`, reproducing the anchored `re_match(..., 0, ...)` behavior required by GNU `expr`.

Each match operator evaluation compiles/evaluates one pattern against one source value. There is no meaningful repeated search over a stable input record.

### Recommendation

Keep the current implementation.

Potential future work should focus on expression-evaluator semantics and compilation reuse only if profiling identifies repeated identical patterns inside one invocation. Prepared byte input is not the relevant mechanism.

**Verdict: already using the right framework abstraction.**

## 6. `numfmt`

### Current shape

`numfmt` uses a source-generated `System.Text.RegularExpressions.Regex` for this pattern:

```text
%(?<flags>[-0']*)(?<width>\d+)?(?:\.(?<precision>\d+))?f
```

This parses `numfmt --format`'s command-specific `%f` directive grammar.

### Analysis

This regex is not user-supplied GNU BRE/ERE/Emacs syntax and does not model GNU regex matching semantics. It is an internal parser implementation detail for a tiny fixed grammar.

`GeneratedRegex` is therefore appropriate:

- compile-time generated;
- culture-invariant;
- fixed and trusted pattern;
- no POSIX leftmost-longest requirement;
- no byte-coordinate or malformed-input contract.

Moving it to `Icod.CommandFramework.RegularExpressions` would confuse mechanism ownership and likely make the code slower and more complex.

A hand-written parser could also be reasonable if desired for strict grammar control, but that is a `numfmt` implementation decision, not a framework dependency issue.

**Verdict: explicitly retain `GeneratedRegex`; do not migrate.**

## Cross-cutting findings

### A. Do not add direct package references to each command merely for 2.2.x

CoreUtils currently consumes `Icod.CommandFramework` through `Icod.CoreUtils.Shared`, and the command projects reference Shared. That transitive package boundary is consistent with the repository's ownership model. A separate CoreUtils change should update the Shared package dependency to the selected 2.2.x release rather than scattering version literals through command projects.

### B. Prepared input is a reuse optimization, not a replacement for every byte match

The preparation API snapshots and preprocesses authoritative bytes. It pays off when the same record/window is matched repeatedly. Single-match records should continue using ordinary `Match` unless measurement proves otherwise.

### C. Preserve each consumer's existing decoding mode

Prepared input must be constructed with the same `RegularExpressionInputOptions` as the ordinary matching path. In particular:

- `ptx` should use `TextDecodingMode.Bytes` to reproduce its current Latin-1 one-byte-unit bridge;
- `tac` should preserve its current input options exactly unless a separate GNU-locale audit justifies a semantic change;
- no migration should accidentally substitute the default UTF-8 mode for an intentionally byte-oriented command.

### D. Framework ownership remains mechanism, not command grammar

None of this audit suggests moving CoreUtils-specific parsing or policy into `Icod.CommandFramework`. The framework should own reusable input preparation and matching mechanism; `ptx`, `tac`, `csplit`, `nl`, `expr`, and `numfmt` continue to own their GNU-visible command semantics.

## Proposed follow-on CoreUtils tranche

After `Icod.CommandFramework 2.2.2` is available:

1. update the centralized CoreUtils Shared dependency from 2.1.0 to 2.2.2;
2. migrate `tac` regex-window enumeration to prepared input and add allocation/performance regression coverage;
3. migrate `ptx` custom word/sentence regex paths from Latin-1 string bridges to byte-mode prepared input, with differential behavior tests;
4. benchmark `csplit` before deciding whether any prepared-input/cache work is worthwhile;
5. leave `nl`, `expr`, and `numfmt` unchanged unless profiling reveals a different bottleneck.

## Release conclusion

`Icod.CommandFramework 2.2.2` does not need a new regex API for these consumers. The useful work at the framework repository level is:

- centralizing the package version in `Directory.Build.props`;
- recording the consumer audit and migration boundaries;
- preserving the 2.2.x prepared-input contract as the canonical solution for repeated authoritative-byte searches.

The next implementation changes belong primarily in `Icod.CoreUtils`, with `ptx` and `tac` as the strongest candidates.
