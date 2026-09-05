namespace Icod.CommandFramework.RegularExpressions;

using System.Buffers;
using System.Globalization;
using System.Text;

/// <summary>
/// Adds immutable prepared-input matching to a compiled GNU regular expression without changing its public API.
/// </summary>
internal sealed class PreparedCompiledRegularExpression :
	ICompiledRegularExpression,
	IPreparedCompiledRegularExpression {
	private readonly int captureCount;
	private readonly IRegularExpressionCharacterClassProvider characterClassProvider;
	private readonly bool completeLiteral;
	private readonly RegexNode expression;
	private readonly ICompiledRegularExpression inner;
	private readonly RegularExpressionOptions options;
	private readonly Rune[] prefix;

	/// <summary>
	/// Initializes a prepared-input wrapper around the existing public compiled expression.
	/// </summary>
	internal PreparedCompiledRegularExpression(
		ICompiledRegularExpression inner,
		string pattern,
		RegexNode expression,
		int captureCount,
		RegularExpressionOptions options,
		IRegularExpressionCharacterClassProvider characterClassProvider
	) {
		ArgumentNullException.ThrowIfNull( inner );
		ArgumentNullException.ThrowIfNull( pattern );
		ArgumentNullException.ThrowIfNull( expression );
		ArgumentNullException.ThrowIfNull( options );
		ArgumentNullException.ThrowIfNull( characterClassProvider );

		this.inner = inner;
		this.expression = expression;
		this.captureCount = captureCount;
		this.options = options;
		this.characterClassProvider = characterClassProvider;

		if ( int.MaxValue != options.MaximumMatchStates ) {
			this.prefix = [];
			return;
		}

		var completePrefix = TryGetPlainLiteralPrefix( pattern );
		if ( 0 < completePrefix.Length && 0 == captureCount ) {
			this.prefix = completePrefix;
			this.completeLiteral = true;
			return;
		}

		this.prefix = RequiredLiteralPrefixAnalyzer.Analyze( pattern );
	}

	/// <inheritdoc/>
	public string Pattern => this.inner.Pattern;

	/// <inheritdoc/>
	public int CaptureCount => this.inner.CaptureCount;

	/// <inheritdoc/>
	public RegularExpressionMatchResult Match(
		string input,
		RegularExpressionMatchOptions? options = null,
		CancellationToken cancellationToken = default
	) => this.inner.Match( input, options, cancellationToken );

	/// <inheritdoc/>
	public ValueTask<RegularExpressionMatchResult> MatchAsync(
		string input,
		RegularExpressionMatchOptions? options = null,
		CancellationToken cancellationToken = default
	) => this.inner.MatchAsync( input, options, cancellationToken );

	/// <inheritdoc/>
	public RegularExpressionByteMatchResult Match(
		ReadOnlyMemory<byte> input,
		RegularExpressionInputOptions? inputOptions = null,
		RegularExpressionByteMatchOptions? matchOptions = null,
		CancellationToken cancellationToken = default
	) => this.inner.Match( input, inputOptions, matchOptions, cancellationToken );

	/// <inheritdoc/>
	public ValueTask<RegularExpressionByteMatchResult> MatchAsync(
		ReadOnlyMemory<byte> input,
		RegularExpressionInputOptions? inputOptions = null,
		RegularExpressionByteMatchOptions? matchOptions = null,
		CancellationToken cancellationToken = default
	) => this.inner.MatchAsync(
		input,
		inputOptions,
		matchOptions,
		cancellationToken
	);

	/// <inheritdoc/>
	public RegularExpressionMatchResult MatchPrepared(
		PreparedRegexInput input,
		RegularExpressionMatchOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( input );
		if ( !input.IsText ) {
			throw new ArgumentException(
				"Prepared input does not contain a .NET string source.",
				nameof( input )
			);
		}

		options ??= new RegularExpressionMatchOptions();
		cancellationToken.ThrowIfCancellationRequested();
		var decoded = input.Decoded;
		if ( !decoded.TryGetUnitIndex( options.StartIndex, out var firstStart ) ) {
			return RegularExpressionMatchResult.Failed(
				new(
					RegularExpressionDiagnosticCode.InvalidStartIndex,
					"start index is outside the input or splits a surrogate pair"
				)
			);
		}

		if ( this.completeLiteral && options.RequireMatchAtStart ) {
			return MatchCompleteTextLiteral(
				decoded,
				firstStart,
				cancellationToken
			);
		}
		if ( 0 < this.prefix.Length && !options.RequireMatchAtStart ) {
			return SearchPreparedTextByPrefix(
				decoded,
				firstStart,
				cancellationToken
			);
		}

		return CreateTextResult(
			decoded,
			Search(
				decoded,
				firstStart,
				options.RequireMatchAtStart,
				cancellationToken
			),
			cancellationToken
		);
	}

	/// <inheritdoc/>
	public RegularExpressionByteMatchResult MatchPrepared(
		PreparedRegexInput input,
		RegularExpressionByteMatchOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( input );
		if ( input.IsText ) {
			throw new ArgumentException(
				"Prepared input does not contain an authoritative byte source.",
				nameof( input )
			);
		}

		options ??= new RegularExpressionByteMatchOptions();
		cancellationToken.ThrowIfCancellationRequested();
		var decoded = input.Decoded;
		if ( !decoded.TryGetUnitIndex( options.StartByteOffset, out var firstStart ) ) {
			return RegularExpressionByteMatchResult.Failed(
				new(
					RegularExpressionDiagnosticCode.InvalidStartByteOffset,
					"start byte offset is outside the input or splits a decoded UTF-8 unit"
				)
			);
		}

		if ( this.completeLiteral && options.RequireMatchAtStart ) {
			return MatchCompleteByteLiteral(
				decoded,
				firstStart,
				cancellationToken
			);
		}
		if ( 0 < this.prefix.Length && !options.RequireMatchAtStart ) {
			return SearchPreparedBytesByPrefix(
				decoded,
				firstStart,
				cancellationToken
			);
		}

		return CreateByteResult(
			decoded,
			Search(
				decoded,
				firstStart,
				options.RequireMatchAtStart,
				cancellationToken
			),
			cancellationToken
		);
	}

	private RegularExpressionMatchResult SearchPreparedTextByPrefix(
		RegexInput input,
		int firstStart,
		CancellationToken cancellationToken
	) {
		var candidateStart = firstStart;
		while ( true ) {
			var candidate = FindNextCandidate(
				input,
				candidateStart,
				cancellationToken
			);
			if ( 0 > candidate ) {
				return RegularExpressionMatchResult.Succeeded( null );
			}
			if ( this.completeLiteral ) {
				return CreateCompleteTextLiteralResult( input, candidate );
			}

			var search = Search( input, candidate, true, cancellationToken );
			if ( search.Diagnostic is not null || search.State is not null ) {
				return CreateTextResult( input, search, cancellationToken );
			}
			candidateStart = candidate + 1;
		}
	}

	private RegularExpressionByteMatchResult SearchPreparedBytesByPrefix(
		RegexInput input,
		int firstStart,
		CancellationToken cancellationToken
	) {
		var candidateStart = firstStart;
		while ( true ) {
			var candidate = FindNextCandidate(
				input,
				candidateStart,
				cancellationToken
			);
			if ( 0 > candidate ) {
				return RegularExpressionByteMatchResult.Succeeded( null );
			}
			if ( this.completeLiteral ) {
				return CreateCompleteByteLiteralResult( input, candidate );
			}

			var search = Search( input, candidate, true, cancellationToken );
			if ( search.Diagnostic is not null || search.State is not null ) {
				return CreateByteResult( input, search, cancellationToken );
			}
			candidateStart = candidate + 1;
		}
	}

	private RegularExpressionMatchResult MatchCompleteTextLiteral(
		RegexInput input,
		int start,
		CancellationToken cancellationToken
	) {
		if ( !MatchesPrefixAt( input, start, cancellationToken ) ) {
			return RegularExpressionMatchResult.Succeeded( null );
		}
		return CreateCompleteTextLiteralResult( input, start );
	}

	private RegularExpressionByteMatchResult MatchCompleteByteLiteral(
		RegexInput input,
		int start,
		CancellationToken cancellationToken
	) {
		if ( !MatchesPrefixAt( input, start, cancellationToken ) ) {
			return RegularExpressionByteMatchResult.Succeeded( null );
		}
		return CreateCompleteByteLiteralResult( input, start );
	}

	private RegularExpressionMatchResult CreateCompleteTextLiteralResult(
		RegexInput input,
		int start
	) {
		var source = input.TextSource!;
		var matchStart = input.GetSourceIndex( start );
		var matchEnd = input.GetSourceIndex( start + this.prefix.Length );
		return RegularExpressionMatchResult.Succeeded(
			new RegularExpressionMatch(
				matchStart,
				matchEnd - matchStart,
				source[ matchStart..matchEnd ],
				Array.Empty<RegularExpressionCapture>()
			)
		);
	}

	private RegularExpressionByteMatchResult CreateCompleteByteLiteralResult(
		RegexInput input,
		int start
	) {
		var source = input.ByteSource;
		var matchStart = input.GetSourceIndex( start );
		var matchEnd = input.GetSourceIndex( start + this.prefix.Length );
		return RegularExpressionByteMatchResult.Succeeded(
			new RegularExpressionByteMatch(
				matchStart,
				matchEnd - matchStart,
				source.Slice( matchStart, matchEnd - matchStart ).ToArray(),
				Array.Empty<RegularExpressionByteCapture>()
			)
		);
	}

	private RegularExpressionMatchResult CreateTextResult(
		RegexInput input,
		RegexSearchResult search,
		CancellationToken cancellationToken
	) {
		if ( search.Diagnostic is RegularExpressionDiagnostic diagnostic ) {
			return RegularExpressionMatchResult.Failed( diagnostic );
		}
		return RegularExpressionMatchResult.Succeeded(
			search.State is null
				? null
				: CreatePublicTextMatch(
					input,
					search.Start,
					search.State,
					cancellationToken
				)
		);
	}

	private RegularExpressionByteMatchResult CreateByteResult(
		RegexInput input,
		RegexSearchResult search,
		CancellationToken cancellationToken
	) {
		if ( search.Diagnostic is RegularExpressionDiagnostic diagnostic ) {
			return RegularExpressionByteMatchResult.Failed( diagnostic );
		}
		return RegularExpressionByteMatchResult.Succeeded(
			search.State is null
				? null
				: CreatePublicByteMatch(
					input,
					search.Start,
					search.State,
					cancellationToken
				)
		);
	}

	private RegexSearchResult Search(
		RegexInput input,
		int firstStart,
		bool requireMatchAtStart,
		CancellationToken cancellationToken
	) {
		try {
			var context = new RegexMatchContext(
				input,
				this.options,
				this.characterClassProvider,
				cancellationToken
			);
			var finalStart = requireMatchAtStart ? firstStart : input.Length;
			for ( var start = firstStart; finalStart >= start; start++ ) {
				cancellationToken.ThrowIfCancellationRequested();
				RegexMatchState? best = null;
				var initial = new RegexMatchState( start, this.captureCount );
				foreach ( var candidate in this.expression.Match( context, initial ) ) {
					if ( best is null || candidate.Position > best.Position ) {
						best = candidate;
					}
				}
				if ( best is RegexMatchState selected ) {
					return new( start, selected, null );
				}
			}
			return new( -1, null, null );
		} catch ( RegexMatchResourceLimitException ) {
			return new(
				-1,
				null,
				new(
					RegularExpressionDiagnosticCode.MatchResourceLimitExceeded,
					string.Concat(
						"regular-expression match exceeded the configured limit of ",
						this.options.MaximumMatchStates.ToString( CultureInfo.InvariantCulture ),
						" states"
					)
				)
			);
		}
	}

	private RegularExpressionMatch CreatePublicTextMatch(
		RegexInput input,
		int start,
		RegexMatchState state,
		CancellationToken cancellationToken
	) {
		var source = input.TextSource!;
		var captures = new RegularExpressionCapture[ this.captureCount ];
		for ( var captureIndex = 0; this.captureCount > captureIndex; captureIndex++ ) {
			cancellationToken.ThrowIfCancellationRequested();
			var capture = state.Captures[ captureIndex ];
			if ( capture is not RegexCaptureSpan captureSpan ) {
				captures[ captureIndex ] = new( false, -1, 0, null );
				continue;
			}
			var captureStart = input.GetSourceIndex( captureSpan.Start );
			var captureEnd = input.GetSourceIndex( captureSpan.End );
			captures[ captureIndex ] = new(
				true,
				captureStart,
				captureEnd - captureStart,
				source[ captureStart..captureEnd ]
			);
		}
		var matchStart = input.GetSourceIndex( start );
		var matchEnd = input.GetSourceIndex( state.Position );
		return new(
			matchStart,
			matchEnd - matchStart,
			source[ matchStart..matchEnd ],
			captures
		);
	}

	private RegularExpressionByteMatch CreatePublicByteMatch(
		RegexInput input,
		int start,
		RegexMatchState state,
		CancellationToken cancellationToken
	) {
		var source = input.ByteSource;
		var captures = new RegularExpressionByteCapture[ this.captureCount ];
		for ( var captureIndex = 0; this.captureCount > captureIndex; captureIndex++ ) {
			cancellationToken.ThrowIfCancellationRequested();
			var capture = state.Captures[ captureIndex ];
			if ( capture is not RegexCaptureSpan captureSpan ) {
				captures[ captureIndex ] = new(
					false,
					-1,
					0,
					ReadOnlyMemory<byte>.Empty
				);
				continue;
			}
			var captureStart = input.GetSourceIndex( captureSpan.Start );
			var captureEnd = input.GetSourceIndex( captureSpan.End );
			captures[ captureIndex ] = new(
				true,
				captureStart,
				captureEnd - captureStart,
				source.Slice( captureStart, captureEnd - captureStart ).ToArray()
			);
		}
		var matchStart = input.GetSourceIndex( start );
		var matchEnd = input.GetSourceIndex( state.Position );
		return new(
			matchStart,
			matchEnd - matchStart,
			source.Slice( matchStart, matchEnd - matchStart ).ToArray(),
			captures
		);
	}

	private bool MatchesPrefixAt(
		RegexInput input,
		int start,
		CancellationToken cancellationToken
	) {
		if ( this.prefix.Length > input.Length - start ) {
			return false;
		}
		for ( var offset = 0; this.prefix.Length > offset; offset++ ) {
			cancellationToken.ThrowIfCancellationRequested();
			var position = start + offset;
			if (
				input.IsOpaque( position )
				|| !this.characterClassProvider.AreCharactersEqual(
					this.prefix[ offset ],
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
		var finalStart = input.Length - this.prefix.Length;
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

	private readonly record struct RegexSearchResult(
		int Start,
		RegexMatchState? State,
		RegularExpressionDiagnostic? Diagnostic
	);
}
