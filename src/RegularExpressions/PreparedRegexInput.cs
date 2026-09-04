namespace Icod.CommandFramework.RegularExpressions;

/// <summary>
/// Provides an immutable prepared regular-expression input with exact source-coordinate mapping.
/// </summary>
internal sealed class PreparedRegexInput {
	private PreparedRegexInput( RegexInput decoded ) {
		Decoded = decoded;
	}

	/// <summary>
	/// Gets the decoded immutable input representation.
	/// </summary>
	internal RegexInput Decoded { get; }

	/// <summary>
	/// Gets whether this input was prepared from a .NET string.
	/// </summary>
	internal bool IsText => Decoded.TextSource is not null;

	/// <summary>
	/// Prepares an immutable .NET string input.
	/// </summary>
	internal static PreparedRegexInput Prepare(
		string source,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( source );
		cancellationToken.ThrowIfCancellationRequested();
		return new( RegexInput.Decode( source, cancellationToken ) );
	}

	/// <summary>
	/// Copies and prepares authoritative bytes so later caller mutation cannot affect matching.
	/// </summary>
	internal static PreparedRegexInput Prepare(
		ReadOnlyMemory<byte> source,
		RegularExpressionInputOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		options ??= new RegularExpressionInputOptions();
		if ( !Enum.IsDefined( options.DecodingMode ) ) {
			throw new ArgumentOutOfRangeException( nameof( options ) );
		}
		if ( !Enum.IsDefined( options.InvalidEncodingPolicy ) ) {
			throw new ArgumentOutOfRangeException( nameof( options ) );
		}
		cancellationToken.ThrowIfCancellationRequested();
		var ownedSource = source.ToArray();
		return new(
			RegexInput.Decode(
				ownedSource,
				options,
				cancellationToken
			)
		);
	}
}
