namespace Icod.CommandFramework.RegularExpressions;

/// <summary>
/// Provides matching operations for immutable prepared authoritative-byte inputs.
/// </summary>
public static class CompiledRegularExpressionPreparedInputExtensions {
	/// <summary>
	/// Searches an immutable prepared authoritative-byte input.
	/// </summary>
	/// <param name="expression">The compiled regular expression.</param>
	/// <param name="input">The immutable prepared input.</param>
	/// <param name="matchOptions">Optional source-byte search positioning policy.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The controlled search result. Match and capture offsets are source-byte offsets.</returns>
	public static RegularExpressionByteMatchResult Match(
		this ICompiledRegularExpression expression,
		RegularExpressionPreparedByteInput input,
		RegularExpressionByteMatchOptions? matchOptions = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( expression );
		ArgumentNullException.ThrowIfNull( input );

		if ( expression is IPreparedCompiledRegularExpression preparedExpression ) {
			return preparedExpression.MatchPrepared(
				input.Prepared,
				matchOptions,
				cancellationToken
			);
		}

		return expression.Match(
			input.Source.ToArray(),
			input.InputOptions,
			matchOptions,
			cancellationToken
		);
	}

	/// <summary>
	/// Asynchronously searches an immutable prepared authoritative-byte input without offloading work to the thread pool.
	/// </summary>
	/// <param name="expression">The compiled regular expression.</param>
	/// <param name="input">The immutable prepared input.</param>
	/// <param name="matchOptions">Optional source-byte search positioning policy.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The controlled search result.</returns>
	public static ValueTask<RegularExpressionByteMatchResult> MatchAsync(
		this ICompiledRegularExpression expression,
		RegularExpressionPreparedByteInput input,
		RegularExpressionByteMatchOptions? matchOptions = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( expression );
		ArgumentNullException.ThrowIfNull( input );

		if ( expression is IPreparedCompiledRegularExpression preparedExpression ) {
			return ValueTask.FromResult(
				preparedExpression.MatchPrepared(
					input.Prepared,
					matchOptions,
					cancellationToken
				)
			);
		}

		return expression.MatchAsync(
			input.Source.ToArray(),
			input.InputOptions,
			matchOptions,
			cancellationToken
		);
	}
}
