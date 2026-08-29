# Input and output

The `Icod.CommandFramework.IO` namespace contains reusable streaming, record, token, pathname-expansion, and temporary-spooling primitives.

## Responsibilities

- Adapt text readers and writers to byte-oriented command implementations.
- Read and write delimited text or byte records incrementally.
- Read byte tokens incrementally using an explicit set of separator bytes.
- Open file operands while preserving the conventional `-` standard-input marker.
- Preserve the synchronous compatibility facade for `*`, `?`, and recursive `**` pathname expansion.
- Copy, compare, skip, and limit streams with bounded memory use.
- Spool data when an operation requires replay without assuming seekable input.

## Pathname expansion compatibility surface

`Icod.CommandFramework.IO.PathnameExpander` is retained for existing consumers that need its synchronous collection API. Its public wildcard contract remains deliberately narrow: `*` and `?` match within one segment, `**` is recursive only as a complete segment, bracket expressions are literal, unmatched patterns are preserved by default, and `-` passes through unchanged.

The implementation delegates filesystem traversal to `Icod.CommandFramework.FileSystem.Traversal.PathnameExpander`; it no longer owns a second directory walker. New code should use the traversal namespace directly, especially when it needs character classes, structured no-match/error/cycle/boundary events, explicit link policy, resource limits, or cancellation.

See [`../FileSystem/Traversal/README.md`](../FileSystem/Traversal/README.md).

## Design notes

APIs are TAP-oriented where I/O is naturally asynchronous, honor cancellation, and do not take ownership of injected standard streams unless an API explicitly says otherwise. `ByteTokenReader` is encoding-agnostic, returns independently owned nonempty tokens, and deliberately has no command-specific pair or graph semantics.

## Record API boundaries

`DelimitedRecordReader` and `DelimitedRecordWriter` are decoded-text conveniences. They operate after a `TextReader` or `TextWriter` has chosen an encoding and may omit, synthesize, or normalize delimiters according to their documented text contract. They cannot recover malformed source bytes or a consumed byte-order mark.

`DelimitedByteRecordReader` is the compatibility whole-record byte API. It continues returning independently owned arrays that include a present separator and preserve a final unterminated record. Its framing now delegates to `Icod.CommandFramework.Records.ByteRecordReader`, which uses `DelimitedByteRecordSegmentReader` internally.

New byte-sensitive commands should use the `Records` namespace directly. `ByteRecordReader` materializes content plus explicit termination metadata when a complete record is required. The segmented reader excludes separators from segment data, reports termination explicitly, bounds each returned segment, and never normalizes carriage returns, line feeds, NUL bytes, encodings, or malformed input.
