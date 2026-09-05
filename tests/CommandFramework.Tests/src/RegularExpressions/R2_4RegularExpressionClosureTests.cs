namespace Icod.CommandFramework.Tests;

using System.Text;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

using Xunit;

public sealed class R2_4RegularExpressionClosureTests {
	private static readonly RegularExpressionInputOptions ByteInputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};

	[Theory]
	[InlineData( false, @"^\(a\|aa\)*b$", "aaaaab" )]
	[InlineData( true, "^(a|aa)*b$", "aaaaab" )]
	[InlineData( false, @"^\(ab\)\1*$", "ababab" )]
	[InlineData( true, @"^(ab)\1*$", "ababab" )]
	[InlineData( false, @"\(a\|ab\)\(c\|cd\)*e", "xxabcdcdeyy" )]
	[InlineData( true, "(a|ab)(c|cd)*e", "xxabcdcdeyy" )]
	public void PreparedAndOrdinaryByteMatchingAgreeForComplexPatterns(
		bool extended,
		string pattern,
		string text
	) {
		var expression = Compile( extended, pattern );
		var source = Encoding.ASCII.GetBytes( text );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			source,
			ByteInputOptions
		);

		var ordinary = expression.Match(
			source,
			ByteInputOptions
		);
		var reused = expression.Match( prepared );

		AssertEquivalent( ordinary, reused );
	}

	[Theory]
	[InlineData( false, "a*" )]
	[InlineData( true, "a*" )]
	public void PreparedAndOrdinaryByteMatchingAgreeForZeroLengthMatches(
		bool extended,
		string pattern
	) {
		var expression = Compile( extended, pattern );
		var source = "bbb"u8.ToArray();
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			source,
			ByteInputOptions
		);
		var options = new RegularExpressionByteMatchOptions {
			StartByteOffset = 1
		};

		var ordinary = expression.Match(
			source,
			ByteInputOptions,
			options
		);
		var reused = expression.Match(
			prepared,
			options
		);

		AssertEquivalent( ordinary, reused );
		Assert.True( ordinary.IsMatch );
		Assert.Equal( 1, ordinary.Match!.ByteIndex );
		Assert.Equal( 0, ordinary.Match.ByteLength );
	}

	[Theory]
	[InlineData( false, @"\(a\|aa\)*b" )]
	[InlineData( true, "(a|aa)*b" )]
	public void PreparedAndOrdinaryByteMatchingPreserveFiniteStateLimitFailures(
		bool extended,
		string pattern
	) {
		var expression = Compile(
			extended,
			pattern,
			new RegularExpressionOptions {
				MaximumMatchStates = 10
			}
		);
		var source = Encoding.ASCII.GetBytes( "aaaaaaaaaaaaaaaa" );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			source,
			ByteInputOptions
		);

		var ordinary = expression.Match(
			source,
			ByteInputOptions
		);
		var reused = expression.Match( prepared );

		Assert.False( ordinary.IsSuccess );
		Assert.False( reused.IsSuccess );
		Assert.Equal(
			RegularExpressionDiagnosticCode.MatchResourceLimitExceeded,
			ordinary.Diagnostic!.Code
		);
		Assert.Equal( ordinary.Diagnostic.Code, reused.Diagnostic!.Code );
	}

	[Theory]
	[InlineData( InvalidEncodingPolicy.PreserveBytes )]
	[InlineData( InvalidEncodingPolicy.Replace )]
	public void PreparedAndOrdinaryByteMatchingAgreeForMalformedUtf8(
		InvalidEncodingPolicy policy
	) {
		var expression = Compile( extended: false, "." );
		var source = new byte[] { 0xff, (byte)'x' };
		var inputOptions = new RegularExpressionInputOptions {
			DecodingMode = TextDecodingMode.Utf8,
			InvalidEncodingPolicy = policy
		};
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			source,
			inputOptions
		);

		var ordinary = expression.Match(
			source,
			inputOptions
		);
		var reused = expression.Match( prepared );

		AssertEquivalent( ordinary, reused );
		Assert.True( ordinary.IsMatch );
		Assert.Equal( new byte[] { 0xff }, ordinary.Match!.Value.ToArray() );
	}

	[Fact]
	public void PreparedAndOrdinaryBytePreparationThrowForMalformedUtf8UnderThrowPolicy() {
		var expression = Compile( extended: false, "." );
		var source = new byte[] { 0xff };
		var inputOptions = new RegularExpressionInputOptions {
			DecodingMode = TextDecodingMode.Utf8,
			InvalidEncodingPolicy = InvalidEncodingPolicy.Throw
		};

		Assert.Throws<DecoderFallbackException>(
			() => expression.Match(
				source,
				inputOptions
			)
		);
		Assert.Throws<DecoderFallbackException>(
			() => RegularExpressionPreparedByteInput.Prepare(
				source,
				inputOptions
			)
		);
	}

	[Theory]
	[InlineData( false, @"\(a\|aa\)*b" )]
	[InlineData( true, "(a|aa)*b" )]
	public void PreparedMatchingHonorsCancellationOnComplexSearch(
		bool extended,
		string pattern
	) {
		var expression = Compile( extended, pattern );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			Encoding.ASCII.GetBytes( new string( 'a', 4096 ) ),
			ByteInputOptions
		);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		Assert.ThrowsAny<OperationCanceledException>(
			() => expression.Match(
				prepared,
				cancellationToken: cancellation.Token
			)
		);
	}

	[Theory]
	[InlineData( false, @"[[:alpha:]][[:digit:]]" )]
	[InlineData( true, "[[:alpha:]][[:digit:]]" )]
	public void PreparedAndOrdinaryByteMatchingAgreeWhenPrefixAccelerationIsUnavailable(
		bool extended,
		string pattern
	) {
		var expression = Compile( extended, pattern );
		var source = Encoding.ASCII.GetBytes( "--x1--" );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			source,
			ByteInputOptions
		);

		var ordinary = expression.Match(
			source,
			ByteInputOptions
		);
		var reused = expression.Match( prepared );

		AssertEquivalent( ordinary, reused );
		Assert.True( ordinary.IsMatch );
		Assert.Equal( 2, ordinary.Match!.ByteIndex );
		Assert.Equal( 2, ordinary.Match.ByteLength );
	}

	private static ICompiledRegularExpression Compile(
		bool extended,
		string pattern,
		RegularExpressionOptions? options = null
	) {
		IRegularExpressionProvider provider = extended
			? new GnuExtendedRegularExpressionProvider(
				PosixCLocaleRegularExpressionCharacterClassProvider.Instance
			)
			: new GnuBasicRegularExpressionProvider(
				PosixCLocaleRegularExpressionCharacterClassProvider.Instance
			)
		;
		var result = provider.Compile( pattern, options );
		Assert.True( result.IsSuccess, result.Diagnostic?.Message );
		return Assert.IsAssignableFrom<ICompiledRegularExpression>( result.Expression );
	}

	private static void AssertEquivalent(
		RegularExpressionByteMatchResult expected,
		RegularExpressionByteMatchResult actual
	) {
		Assert.Equal( expected.IsSuccess, actual.IsSuccess );
		Assert.Equal( expected.IsMatch, actual.IsMatch );
		Assert.Equal( expected.Diagnostic?.Code, actual.Diagnostic?.Code );
		if ( expected.Match is null ) {
			Assert.Null( actual.Match );
			return;
		}

		Assert.NotNull( actual.Match );
		Assert.Equal( expected.Match.ByteIndex, actual.Match.ByteIndex );
		Assert.Equal( expected.Match.ByteLength, actual.Match.ByteLength );
		Assert.Equal( expected.Match.Value.ToArray(), actual.Match.Value.ToArray() );
		Assert.Equal( expected.Match.Captures.Count, actual.Match.Captures.Count );
		for ( var index = 0; expected.Match.Captures.Count > index; index++ ) {
			var expectedCapture = expected.Match.Captures[ index ];
			var actualCapture = actual.Match.Captures[ index ];
			Assert.Equal( expectedCapture.Success, actualCapture.Success );
			Assert.Equal( expectedCapture.ByteIndex, actualCapture.ByteIndex );
			Assert.Equal( expectedCapture.ByteLength, actualCapture.ByteLength );
			Assert.Equal(
				expectedCapture.Value.ToArray(),
				actualCapture.Value.ToArray()
			);
		}
	}
}
