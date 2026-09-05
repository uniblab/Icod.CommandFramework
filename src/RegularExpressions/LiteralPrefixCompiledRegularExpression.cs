namespace Icod.CommandFramework.RegularExpressions;

using System.Buffers;
using System.Text;

/// <summary>
/// Accelerates searches when a required leading literal run can be proven conservatively.
/// </summary>
internal sealed class LiteralPrefixCompiledRegularExpression : ICompiledRegularExpression {
	private readonly ICompiledRegularExpression inner;
	private readonly Rune[] prefix;
	private readonly bool completeLiteral;
	private readonly RegularExpressionOptions options;
	private readonly IRegularExpressionCharacterClassProvider characterClassProvider;

	private LiteralPrefixCompiledRegularExpression(
		ICompiledRegularExpression inner,
		Rune[] prefix,
		bool completeLiteral,
		RegularExpressionOptions options,
		IRegularExpressionCharacterClassProvider characterClassProvider
	) {
		this.inner = inner;
		this.prefix = prefix;
		this.completeLiteral = completeLiteral;
		this.options = options;
		this.characterClassProvider = characterClassProvider;
	}

	/// <summary>
	/// Creates an accelerated wrapper when a required leading literal run can be proven safely.
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

		if ( int.MaxValue != options.MaximumMatchStates ) {
			return inner;
		}

		var completePrefix = TryGetPlainLiteralPrefix( pattern );
		if ( 0 < completePrefix.Length && 0 == inner.CaptureCount ) {
			return new LiteralPrefixCompiledRegularExpression(
				inner,
				completePrefix,
				true,
				options,
				characterClassProvider
			);
		}

		var requiredPrefix = RequiredLiteralPrefixAnalyzer.Analyze( pattern );
		return 0 == requiredPrefix.Length
			? inner
			: new LiteralPrefixCompiledRegularExpression(
				inner,
				requiredPrefix,
				false,
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
		if ( options.RequireMatchAtStart && !completeLiteral ) {
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

		if ( options.RequireMatchAtStart ) {
			if ( !MatchesPrefixAt( decodedInput, firstStart, cancellationToken ) ) {
				return RegularExpressionMatchResult.Succeeded( null );
			}
			var matchStart = decodedInput.GetSourceIndex( firstStart );
			var matchEnd = decodedInput.GetSourceIndex( firstStart + prefix.Length );
			return RegularExpressionMatchResult.Succeeded(
				new RegularExpressionMatch(
					matchStart,
					matchEnd - matchStart,
					input[ matchStart..matchEnd ],
					Array.Empty<RegularExpressionCapture>()
				)
			);
		}

		var candidateStart = firstStart;
		while ( true ) {
			var candidate = FindNextCandidate(
				decodedInput,
				candidateStart,
				cancellationToken
			);
			if ( 0 > candidate ) {
				return RegularExpressionMatchResult.Succeeded( null );
			}

			var matchStart = decodedInput.GetSourceIndex( candidate );
			if ( completeLiteral ) {
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

			var result = inner.Match(
				input,
				options with {
					StartIndex = matchStart,
					RequireMatchAtStart = true
				},
				cancellationToken
			);
			if ( !result.IsSuccess || result.IsMatch ) {
				return result;
			}
			candidateStart = candidate + 1;
		}
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
		if ( matchOptions.RequireMatchAtStart && !completeLiteral ) {
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

		if ( matchOptions.RequireMatchAtStart ) {
			if ( !MatchesPrefixAt( decodedInput, firstStart, cancellationToken ) ) {
				return RegularExpressionByteMatchResult.Succeeded( null );
			}
			var matchStart = decodedInput.GetSourceIndex( firstStart );
			var matchEnd = decodedInput.GetSourceIndex( firstStart + prefix.Length );
			return RegularExpressionByteMatchResult.Succeeded(
				new RegularExpressionByteMatch(
					matchStart,
					matchEnd - matchStart,
					input.Slice( matchStart, matchEnd - matchStart ),
					Array.Empty<RegularExpressionByteCapture>()
				)
			);
		}

		var candidateStart = firstStart;
		while ( true ) {
			var candidate = FindNextCandidate(
				decodedInput,
				candidateStart,
				cancellationToken
			);
			if ( 0 > candidate ) {
				return RegularExpressionByteMatchResult.Succeeded( null );
			}

			var matchStart = decodedInput.GetSourceIndex( candidate );
			if ( completeLiteral ) {
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

			var result = inner.Match(
				input,
				inputOptions,
				matchOptions with {
					StartByteOffset = matchStart,
					RequireMatchAtStart = true
				},
				cancellationToken
			);
			if ( !result.IsSuccess || result.IsMatch ) {
				return result;
			}
			candidateStart = candidate + 1;
		}
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

	private bool MatchesPrefixAt(
		RegexInput input,
		int start,
		CancellationToken cancellationToken
	) {
		if ( prefix.Length > input.Length - start ) {
			return false;
		}
		for ( var offset = 0; prefix.Length > offset; offset++ ) {
			cancellationToken.ThrowIfCancellationRequested();
			var position = start + offset;
			if (
				input.IsOpaque( position )
				|| !characterClassProvider.AreCharactersEqual(
					prefix[ offset ],
					input[ position ],
					this.options.IgnoreCase
				)
			) {
				return false;
			}
		}
		return true;
	}

	private int FindNextCandidate(
		RegexInput input,
		int firstStart,
		CancellationToken cancellationToken
	) {
		var finalStart = input.Length - prefix.Length;
		for ( var start = firstStart; finalStart >= start; start++ ) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( MatchesPrefixAt( input, start, cancellationToken ) ) {
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
