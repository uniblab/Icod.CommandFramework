namespace Icod.CommandFramework.RegularExpressions;

using System.Buffers;
using System.Text;

/// <summary>
/// Accelerates unanchored searches for expressions whose complete pattern is a provable literal sequence.
/// </summary>
internal sealed class LiteralPrefixCompiledRegularExpression : ICompiledRegularExpression {
	private readonly ICompiledRegularExpression inner;
	private readonly Rune[] prefix;
	private readonly RegularExpressionOptions options;
	private readonly IRegularExpressionCharacterClassProvider characterClassProvider;

	private LiteralPrefixCompiledRegularExpression(
		ICompiledRegularExpression inner,
		Rune[] prefix,
		RegularExpressionOptions options,
		IRegularExpressionCharacterClassProvider characterClassProvider
	) {
		this.inner = inner;
		this.prefix = prefix;
		this.options = options;
		this.characterClassProvider = characterClassProvider;
	}

	/// <summary>
	/// Creates an accelerated wrapper when the pattern is provably a plain literal sequence.
	/// </summary>
	internal static ICompiledRegularExpression Create(
		ICompiledRegularExpression inner,
		string pattern,
		RegularExpressionOptions options,
		IRegularExpressionCharacterClassProvider characterClassProvider
	) {
		ArgumentNullException.ThrowIfNull( inner );
		ArgumentNullException.ThrowIfNull( pattern );
		ArgumentNullException.ThrowIfNull( options );
		ArgumentNullException.ThrowIfNull( characterClassProvider );

		if ( int.MaxValue != options.MaximumMatchStates || 0 != inner.CaptureCount ) {
			return inner;
		}

		var prefix = TryGetPlainLiteralPrefix( pattern );
		return 0 == prefix.Length
			? inner
			: new LiteralPrefixCompiledRegularExpression(
				inner,
				prefix,
				options,
				characterClassProvider
			);
	}

	/// <inheritdoc/>
	public string Pattern => inner.Pattern;

	/// <inheritdoc/>
	public int CaptureCount => inner.CaptureCount;

	/// <inheritdoc/>
	public RegularExpressionMatchResult Match(
		string input,
		RegularExpressionMatchOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( input );
		options ??= new RegularExpressionMatchOptions();
		if ( options.RequireMatchAtStart ) {
			return inner.Match( input, options, cancellationToken );
		}

		cancellationToken.ThrowIfCancellationRequested();
		var decodedInput = RegexInput.Decode( input, cancellationToken );
		if ( !decodedInput.TryGetUnitIndex( options.StartIndex, out var firstStart ) ) {
			return RegularExpressionMatchResult.Failed(
				new(
					RegularExpressionDiagnosticCode.InvalidStartIndex,
					"start index is outside the input or splits a surrogate pair"
				)
			);
		}

		var candidate = FindNextCandidate(
			decodedInput,
			firstStart,
			cancellationToken
		);
		if ( 0 > candidate ) {
			return RegularExpressionMatchResult.Succeeded( null );
		}

		var matchStart = decodedInput.GetSourceIndex( candidate );
		var matchEnd = decodedInput.GetSourceIndex( candidate + prefix.Length );
		return RegularExpressionMatchResult.Succeeded(
			new RegularExpressionMatch(
				matchStart,
				matchEnd - matchStart,
				input[ matchStart..matchEnd ],
				Array.Empty<RegularExpressionCapture>()
			)
		);
	}

	/// <inheritdoc/>
	public ValueTask<RegularExpressionMatchResult> MatchAsync(
		string input,
		RegularExpressionMatchOptions? options = null,
		CancellationToken cancellationToken = default
	) => ValueTask.FromResult( Match( input, options, cancellationToken ) );

	/// <inheritdoc/>
	public RegularExpressionByteMatchResult Match(
		ReadOnlyMemory<byte> input,
		RegularExpressionInputOptions? inputOptions = null,
		RegularExpressionByteMatchOptions? matchOptions = null,
		CancellationToken cancellationToken = default
	) {
		inputOptions ??= new RegularExpressionInputOptions();
		matchOptions ??= new RegularExpressionByteMatchOptions();
		if ( !Enum.IsDefined( inputOptions.DecodingMode ) ) {
			throw new ArgumentOutOfRangeException( nameof( inputOptions ) );
		}
		if ( !Enum.IsDefined( inputOptions.InvalidEncodingPolicy ) ) {
			throw new ArgumentOutOfRangeException( nameof( inputOptions ) );
		}
		if ( matchOptions.RequireMatchAtStart ) {
			return inner.Match(
				input,
				inputOptions,
				matchOptions,
				cancellationToken
			);
		}

		cancellationToken.ThrowIfCancellationRequested();
		var decodedInput = RegexInput.Decode(
			input,
			inputOptions,
			cancellationToken
		);
		if ( !decodedInput.TryGetUnitIndex( matchOptions.StartByteOffset, out var firstStart ) ) {
			return RegularExpressionByteMatchResult.Failed(
				new(
					RegularExpressionDiagnosticCode.InvalidStartByteOffset,
					"start byte offset is outside the input or splits a decoded UTF-8 unit"
				)
			);
		}

		var candidate = FindNextCandidate(
			decodedInput,
			firstStart,
			cancellationToken
		);
		if ( 0 > candidate ) {
			return RegularExpressionByteMatchResult.Succeeded( null );
		}

		var matchStart = decodedInput.GetSourceIndex( candidate );
		var matchEnd = decodedInput.GetSourceIndex( candidate + prefix.Length );
		return RegularExpressionByteMatchResult.Succeeded(
			new RegularExpressionByteMatch(
				matchStart,
				matchEnd - matchStart,
				input.Slice( matchStart, matchEnd - matchStart ),
				Array.Empty<RegularExpressionByteCapture>()
			)
		);
	}

	/// <inheritdoc/>
	public ValueTask<RegularExpressionByteMatchResult> MatchAsync(
		ReadOnlyMemory<byte> input,
		RegularExpressionInputOptions? inputOptions = null,
		RegularExpressionByteMatchOptions? matchOptions = null,
		CancellationToken cancellationToken = default
	) => ValueTask.FromResult(
		Match( input, inputOptions, matchOptions, cancellationToken )
	);

	private int FindNextCandidate(
		RegexInput input,
		int firstStart,
		CancellationToken cancellationToken
	) {
		var finalStart = input.Length - prefix.Length;
		for ( var start = firstStart; finalStart >= start; start++ ) {
			cancellationToken.ThrowIfCancellationRequested();
			var matches = true;
			for ( var offset = 0; prefix.Length > offset; offset++ ) {
				var position = start + offset;
				if (
					input.IsOpaque( position )
					|| !characterClassProvider.AreCharactersEqual(
						prefix[ offset ],
						input[ position ],
						this.options.IgnoreCase
					)
				) {
					matches = false;
					break;
				}
			}
			if ( matches ) {
				return start;
			}
		}
		return -1;
	}

	private static Rune[] TryGetPlainLiteralPrefix( string pattern ) {
		if ( 0 == pattern.Length ) {
			return [];
		}

		var values = new List<Rune>( pattern.Length );
		var index = 0;
		while ( pattern.Length > index ) {
			var status = Rune.DecodeFromUtf16(
				pattern.AsSpan( index ),
				out var value,
				out var consumed
			);
			if ( OperationStatus.Done != status || IsPotentialOperator( value ) ) {
				return [];
			}
			values.Add( value );
			index += consumed;
		}
		return [ .. values ];
	}

	private static bool IsPotentialOperator( Rune value ) => value.Value switch {
		'\\' or '.' or '[' or ']' or '*' or '^' or '$'
			or '(' or ')' or '{' or '}' or '?' or '+' or '|' => true,
		_ => false
	};
}
