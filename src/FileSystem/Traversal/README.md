# Filesystem traversal and pathname expansion

`Icod.CommandFramework.FileSystem.Traversal` contains the canonical read-only filesystem observation, pathname-pattern, glob-expansion, and traversal infrastructure used by `Icod.CommandFramework`.

The namespace deliberately separates four responsibilities:

1. platform pathname grammar and decomposition, supplied by `Icod.Path`;
2. segment-aware pathname-pattern parsing and matching;
3. injectable one-level filesystem observation and pathname expansion; and
4. iterative, policy-driven recursive traversal.

Command implementations remain responsible for deciding which operands are eligible for expansion and what the resulting pathnames mean. This namespace contains no command-specific diagnostic formatting, output policy, or exit-status policy.

## Pathname grammar boundary

`Icod.Path` is the authority for pathname grammar. `PathnamePattern` uses `Icod.Path.PathSyntaxParser` and `PathPlatformSemantics` for roots, separators, volume identity, host comparison conventions, and component decomposition.

`Icod.CommandFramework` remains the authority for wildcard meaning, matching, directory enumeration, recursive expansion, link policy, ordering, resource limits, and unmatched-pattern behavior.

This is pathname evaluation rather than canonicalization. Expansion preserves operational pathname spelling and does not implicitly perform a complete `realpath`-style resolution.

## Pattern grammar

`PathnamePattern` and `PathnamePatternMatcher` implement the canonical segment-aware pattern language.

| Syntax | Meaning |
| --- | --- |
| `*` | Zero or more characters within one pathname segment. |
| `?` | Exactly one character within one pathname segment. |
| `**` | Zero or more complete pathname segments, but only when the whole segment is exactly `**`. |
| `[abc]` | One character from the listed set. |
| `[a-z]` | One character from the inclusive range. |
| `[!a-z]` / `[^a-z]` | One character not in the class. |

Ordinary `*` and `?` never consume a pathname separator. Multiple ordinary asterisks inside a segment do not acquire recursive meaning; for example, `a**b` is still an ordinary one-segment pattern.

Malformed or unterminated character classes are treated as literal text rather than guessed into another pattern.

Wildcard matching of a leading period is controlled independently. The default `LeadingPeriodPolicy.RequireExplicitPeriod` prevents wildcard tokens from matching a leading `.` unless the pattern segment begins with a literal period. `LeadingPeriodPolicy.WildcardMayMatch` opts into matching leading-period names.

`PathCaseSensitivity.Automatic` follows host pathname comparison: ordinal case-insensitive on Windows and ordinal case-sensitive elsewhere. Backslash quoting is enabled by default on Unix-like hosts and disabled by default on Windows, where backslash is a pathname separator.

## Matching without filesystem access

Use `PathnamePattern` or `PathnamePatternMatcher` when only lexical pathname matching is required.

```csharp
using Icod.CommandFramework.FileSystem.Traversal;

var pattern = PathnamePattern.Parse(
	"src/**/*.cs"
);

if ( pattern.IsMatch( "src/Commands/Program.cs" ) ) {
	// Lexical pathname match only; no filesystem was observed.
}
```

## Canonical pathname expansion

`PathnameExpander` is the authoritative filesystem expansion engine. It accepts only operands that a command has already classified as eligible pathnames and expands them through an injected `IReadOnlyFileSystemProvider`.

`ExpandAsync` returns provenance-preserving `PathnameExpansionEvent` values. An event can represent:

- an expanded or preserved `Root`;
- `NoMatch`;
- a structured `Error`;
- an active-ancestry `Cycle`; or
- a `FileSystemBoundary`.

Expansion preserves original operand text, operand order, repeated operands, result ordinals, operational paths, and display paths.

Current-directory segments are evaluated lexically without changing the operational directory and remain present in the display path. Parent-directory segments navigate through the provider while rewinding active expansion ancestry.

### Unmatched patterns

`UnmatchedPathnamePatternBehavior` makes no-match policy explicit:

- `PreserveAsLiteral` returns the original operand as a literal root;
- `ReturnNoMatches` returns a no-match event without inventing a root; and
- `ReportError` returns a structured error.

The default is `PreserveAsLiteral`.

### Ordering

Canonical expansion is deterministic by default:

- ordinal ordering on POSIX-like hosts; and
- ordinal case-insensitive ordering on Windows.

`PathnameExpansionMatchOrder.Provider` remains available when provider enumeration order is deliberately required.

### Command-oriented collection expansion

`ExpandOperandsAsync` collects the canonical event stream into a `PathnameOperandExpansionResult`.

```csharp
var expander = new PathnameExpander(
	SystemReadOnlyFileSystemProvider.Instance
);

var result = await expander.ExpandOperandsAsync(
	operands,
	new PathnameOperandExpansionOptions {
		ExpansionOptions = new PathnameExpansionOptions {
			UnmatchedPatternBehavior =
				UnmatchedPathnamePatternBehavior.PreserveAsLiteral
		},
		IncludeFiles = true,
		IncludeDirectories = false
	},
	cancellationToken
);

foreach ( var pathname in result.Operands ) {
	// Apply command-specific meaning to the expanded operand.
}

foreach ( var issue in result.Issues ) {
	// Apply command-specific diagnostic or exit-status policy.
}
```

File/directory filtering applies only to actual expanded matches. Literal operands retain their display spelling and are not required to exist. This intentionally preserves conventional operands such as `-` and leaves literal-path validation to the command or later filesystem operation.

## Provider boundary

`IReadOnlyFileSystemProvider` exposes only:

- observation of one pathname under an explicit terminal `PathDereferenceMode`; and
- enumeration of one directory level.

The historical Boolean observation overload remains source-compatible, but new consumers use `NoFollow` or `FollowEligiblePathIndirection`.

`SystemReadOnlyFileSystemProvider` supplies the host implementation. Stable entry and filesystem identities are obtained through:

- Windows file identifiers and volume serial numbers;
- Linux `statx` device/inode and mount identifiers; and
- macOS `stat`/`lstat` device and inode values.

An unavailable identity is represented explicitly. Recursive traversal and recursive `**` expansion require stable directory identities for active-ancestry cycle safety. Root-filesystem confinement additionally requires filesystem identities.

Finite nonrecursive pathname expansion remains bounded by its segment count, can proceed without entry identities, and does not reject a finite path merely because it revisits an ancestor through an explicitly followed link.

The provider does not recurse, filter, format diagnostics, suppress exceptions, or decide command exit status. Tests can inject a deterministic provider without touching the host filesystem.

## Recursive expansion and pathname indirection

Recursive `**` expansion requires stable directory identities so cycles can be detected safely. If a required identity is unavailable, expansion returns a structured `IdentityUnavailable` error rather than performing unbounded recursion.

`SymbolicLinkTraversalMode` controls eligible intermediate pathname indirection:

- `Never` does not follow directory links discovered during expansion;
- `RootsOnly` follows explicitly named intermediate links but not wildcard-discovered intermediate links; and
- `Always` allows eligible pathname indirection to be followed subject to cycle and boundary controls.

A terminal wildcard match is an expanded root. A trailing separator may therefore require terminal dereference to determine whether that root is directory-like. `**` itself never changes link-following policy.

Through the neutral `Icod.Path` inspector, Windows symbolic links, directory junctions, and mounted volumes may be followed when policy permits. Unknown name surrogates are not followed. Recognized non-name-surrogate points, including Cloud Files placeholders and opaque filter-managed objects, retain their underlying file or directory kind and are not treated as links.

No-follow entries preserve `PathIndirectionInfo`, including Windows reparse tags, junction-versus-mounted-volume classification, provider-normalized and raw targets, name-surrogate status, and recall/offline attributes.

## Traversal

`ReadOnlyPathTraversalEngine` consumes provenance-preserving `PathTraversalRoot` objects and yields `PathTraversalEvent` values. Events distinguish:

- root start;
- directory preorder entry;
- nondirectory entries;
- directory postorder exit;
- structured errors;
- active-ancestry cycles; and
- filesystem boundaries.

Traversal is iterative rather than recursive, so managed call-stack depth does not grow with pathname depth. Each active directory frame retains at most one configured bounded child set, permitting deterministic ordering without materializing the complete tree.

Cycle detection uses identities in the active directory ancestry. It is deliberately not a global visited-object set: repeated explicit roots, hard-linked nondirectories, and independently reached directory identities remain observable. A followed directory that identifies an active ancestor produces a cycle event and is not descended into again.

`IPathTraversalSelector` returns independent yield and descend decisions. `PathTraversalRuleSelector` supplies ordered last-matching-rule behavior over basename, root-relative path, whole operational path, or matching-name suffixes. Directory pruning occurs before enumeration.

## Filesystem boundaries and resource limits

`FileSystemBoundaryMode.StayOnRootFileSystem` compares each directory's filesystem identity with the root identity before descent. A different identity produces a boundary event. An unavailable required identity produces a structured error.

`MaximumDepth` and `MaximumEntriesPerDirectory` bound expansion work. Directory-entry limits, observation failures, unavailable identities, and other expected traversal failures remain structured events rather than silently changing pathname meaning.

## Error, cancellation, and ownership policy

Expansion and traversal do not write diagnostics. Consumers decide quoting, suppression, quiet behavior, continuation, and exit status.

Expansion and traversal expose cancellation-aware `IAsyncEnumerable<T>` APIs. Cancellation is checked before and between observations, enumerated entries, policy decisions, descents, and yielded events. Cancellation propagates as `OperationCanceledException`; it is not converted into a no-match or traversal error.

The traversal layer owns no command streams and opens no persistent caller-visible handles. The system provider's native observation handles are scoped to one observation.

## Legacy IO facade

`Icod.CommandFramework.IO.PathnameExpander` remains for source compatibility with existing synchronous consumers. It delegates traversal to the canonical `FileSystem.Traversal.PathnameExpander` but intentionally preserves its narrower historical contract:

- only `*`, `?`, and complete-segment `**` are wildcard syntax;
- bracket expressions remain literal;
- leading-period names may be matched by legacy wildcards;
- unmatched wildcard operands are preserved by default;
- `-` passes through unchanged; and
- the API is synchronous and returns pathname strings rather than structured events.

New pathname-expansion code should normally use `Icod.CommandFramework.FileSystem.Traversal` directly.

## Layer boundaries

This namespace supplies the minimum information needed for safe read-only pathname expansion and traversal: effective entry kind, pathname-indirection/reparse characterization, stable entry identity, and filesystem identity.

Authoritative filesystem metadata remains in the sibling [`Metadata`](../Metadata/README.md) namespace. Mutation policy and race-resistant filesystem changes remain in their corresponding mutation layers. Full lexical and physical canonicalization remains in `Icod.Path`.

Commands must not use traversal expansion as an implicit `realpath` implementation or as a substitute for metadata and mutation contracts.
