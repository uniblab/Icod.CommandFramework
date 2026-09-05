namespace Icod.CommandFramework.RegularExpressions;

/// <summary>
/// Provides the internal prepared-input matching contract used by performance-sensitive shared consumers.
/// </summary>
internal interface IPreparedCompiledRegularExpression {
	/// <summary>
	/// Searches an already-prepared immutable string input.
	/// </summary>
	RegularExpressionMatchResult MatchPrepared(
		PreparedRegexInput input,
		RegularExpressionMatchOptions? options = null,
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Searches an already-prepared immutable authoritative-byte input.
	/// </summary>
	RegularExpressionByteMatchResult MatchPrepared(
		PreparedRegexInput input,
		RegularExpressionByteMatchOptions? options = null,
		CancellationToken cancellationToken = default
	);
}
