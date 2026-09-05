# Shared regular-expression tests

This directory verifies the command-neutral GNU regular-expression foundation in `Icod.CommandFramework.RegularExpressions`.

- `GnuBasicRegularExpressionTests.cs` protects the GNU/POSIX Basic profile and established Coreutils consumers.
- `GnuExtendedRegularExpressionTests.cs` protects the GNU/POSIX Extended profile used by search consumers.
- `GnuEmacsRegularExpressionTests.cs` protects the Gnulib Emacs profile used by `ptx`.
- `LiteralPrefixRegularExpressionTests.cs` protects the conservative R2.1 literal/required-prefix acceleration, including anchored behavior, finite-resource-limit fallback, malformed-input handling, and syntax-profile equivalence.
- `PreparedRegularExpressionTests.cs` protects R2.3 prepared-input parity, defensive byte ownership, returned-result isolation, concurrent reuse, exact UTF-8/UTF-16 coordinates, malformed-input behavior, and finite state accounting.
- `PreparedRegularExpressionProfileTests.cs` verifies prepared matching across BRE, ERE, and Emacs profiles and adds cancellation, encoding-policy, and input-kind guardrails.
- `LineEditorRegularExpressionContractTests.cs` is the Phase LE2 acceptance suite. It validates the Shared BRE/ERE boundary required by GNU Sed 4.10 and GNU Ed 1.22.5 without moving Sed- or Ed-specific state into Shared.

R2.2 does not introduce a second semantic matcher. Deterministic `SequenceRegexNode` instances may use the optimized single-state path only when every direct child is proven single-successor and non-capturing and `MaximumMatchStates` is unlimited. Branching, capturing, repetition, backreferences, unproven node types, and finite resource limits retain the general collection path at the containing sequence level. The full BRE/ERE/Emacs suite remains the semantic authority for both paths.

R2.3 Candidate 1 adds an internal immutable prepared-input seam without changing the public matching API. Byte preparation takes defensive ownership of its source, prepared state carries no mutable per-match context, and each call creates independent match state. Tests therefore exercise both ordinary semantic parity and the stronger requirement that one compiled expression and one prepared input remain safe for concurrent reuse.

The LE2 suite intentionally stops at compilation, matching, captures, coordinates, locale policy, diagnostics, cancellation, and resource limits. LE4 adds one consumer-evidence regression proving that line-sensitive anchors, dot, and negated brackets honor a caller-selected NUL separator without changing the LF defaults. Empty-pattern reuse, address-versus-substitution context, repeated-match progression, replacement parsing, and output encoding remain consumer responsibilities.
