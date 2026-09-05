namespace Icod.CommandFramework.RegularExpressions;

/// <summary>
/// Represents an immutable prepared authoritative-byte input that can be reused across regular-expression matches.
/// </summary>
public sealed class RegularExpressionPreparedByteInput {
	private readonly byte[] source;

	private RegularExpressionPreparedByteInput(
		byte[] source,
		RegularExpressionInputOptions inputOptions,
		PreparedRegexInput prepared
	) {
		this.source = source;
		InputOptions = inputOptions;
		Prepared = prepared;
	}

	/// <summary>
	/// Gets the authoritative source-byte length.
	/// </summary>
	public int Length => this.source.Length;

	/// <summary>
	/// Gets the immutable byte-decoding policy captured during preparation.
	/// </summary>
	public RegularExpressionInputOptions InputOptions { get; }

	/// <summary>
	/// Prepares an immutable authoritative-byte input for repeated regular-expression matching.
	/// </summary>
	/// <param name="source">The authoritative source bytes. The bytes are copied before decoding.</param>
	/// <param name="inputOptions">Optional byte-decoding and invalid-input policy.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>An immutable prepared input that owns its source-byte snapshot.</returns>
	/// <exception cref="System.Text.DecoderFallbackException">Malformed UTF-8 is encountered under the throw policy.</exception>
	public static RegularExpressionPreparedByteInput Prepare(
		ReadOnlyMemory<byte> source,
		RegularExpressionInputOptions? inputOptions = null,
		CancellationToken cancellationToken = default
	) {
		inputOptions ??= new RegularExpressionInputOptions();
		if ( !Enum.IsDefined( inputOptions.DecodingMode ) ) {
			throw new ArgumentOutOfRangeException( nameof( inputOptions ) );
		}
		if ( !Enum.IsDefined( inputOptions.InvalidEncodingPolicy ) ) {
			throw new ArgumentOutOfRangeException( nameof( inputOptions ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		var ownedSource = source.ToArray();
		return new(
			ownedSource,
			inputOptions,
			PreparedRegexInput.PrepareOwned(
				ownedSource,
				inputOptions,
				cancellationToken
			)
		);
	}

	internal ReadOnlyMemory<byte> Source => this.source;

	internal PreparedRegexInput Prepared { get; }
}
