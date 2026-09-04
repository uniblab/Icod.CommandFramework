namespace Icod.CommandFramework.Tests;

using System.Runtime.InteropServices;
using System.Text;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

using Xunit;

public sealed class PreparedRegularExpressionTests {
	private static readonly RegularExpressionInputOptions ByteInputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};

	[Theory]
	[InlineData( "TARGET", "xxTARGETyy" )]
	[InlineData( @"\(TARGET\|OTHER\)", "xxOTHERyy" )]
	[InlineData( @"TAR\(GE\)\?T", "xxTARGETyy" )]
	[InlineData( @"\bTARGET\b", "x TARGET y" )]
	[InlineData( @"\(ab\)\1", "xxababyy" )]
	[InlineData( @"TA\{1,2\}RGET", "xxTARGETyy" )]
	public void PreparedByteMatchingAgreesWithPublicMatching(
		string pattern,
		string text
	) {
		var expression = Compile( pattern );
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>( expression );
		var source = Encoding.ASCII.GetBytes( text );
		var prepared = PreparedRegexInput.Prepare( source, ByteInputOptions );

		var expected = expression.Match( source, ByteInputOptions );
		var actual = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions()
		);

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

	[Fact]
	public void PreparedByteInputOwnsAndProtectsItsAuthoritativeSource() {
		var expression = Compile( "TARGET" );
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>( expression );
		var source = "xxTARGETyy"u8.ToArray();
		var prepared = PreparedRegexInput.Prepare( source, ByteInputOptions );
		source[ 2 ] = (byte)'X';

		var first = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions()
		);
		Assert.True( first.IsMatch );
		Assert.Equal( "TARGET"u8.ToArray(), first.Match!.Value.ToArray() );

		Assert.True(
			MemoryMarshal.TryGetArray(
				first.Match.Value,
				out ArraySegment<byte> exposedResult
			)
		);
		exposedResult.Array![ exposedResult.Offset ] = (byte)'X';

		var second = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions()
		);
		Assert.True( second.IsMatch );
		Assert.Equal( "TARGET"u8.ToArray(), second.Match!.Value.ToArray() );
	}

	[Fact]
	public async Task PreparedByteInputSupportsConcurrentMatches() {
		var expression = Compile( "TARGET" );
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>( expression );
		var source = Encoding.ASCII.GetBytes( "TARGET----TARGET----TARGET" );
		var prepared = PreparedRegexInput.Prepare( source, ByteInputOptions );
		var offsets = new[] { 0, 1, 10, 11, 20 };

		var tasks = Enumerable.Range( 0, 128 )
			.Select(
				index => Task.Run(
					() => matcher.MatchPrepared(
						prepared,
						new RegularExpressionByteMatchOptions {
							StartByteOffset = offsets[ index % offsets.Length ]
						}
					)
				)
			)
			.ToArray();
		var results = await Task.WhenAll( tasks );

		for ( var index = 0; results.Length > index; index++ ) {
			var result = results[ index ];
			Assert.True( result.IsSuccess );
			Assert.True( result.IsMatch );
			var expectedIndex = offsets[ index % offsets.Length ] switch {
				0 => 0,
				1 or 10 => 10,
				11 or 20 => 20,
				_ => throw new InvalidOperationException()
			};
			Assert.Equal( expectedIndex, result.Match!.ByteIndex );
			Assert.Equal( "TARGET"u8.ToArray(), result.Match.Value.ToArray() );
		}
	}

	[Fact]
	public void PreparedUtf8InputPreservesExactSourceBoundaries() {
		var expression = Compile( "TARGET" );
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>( expression );
		var source = Encoding.UTF8.GetBytes( "éTARGET" );
		var prepared = PreparedRegexInput.Prepare(
			source,
			new RegularExpressionInputOptions {
				DecodingMode = TextDecodingMode.Utf8
			}
		);

		var matched = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions { StartByteOffset = 2 }
		);
		Assert.True( matched.IsMatch );
		Assert.Equal( 2, matched.Match!.ByteIndex );
		Assert.Equal( 6, matched.Match.ByteLength );

		var split = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions { StartByteOffset = 1 }
		);
		Assert.False( split.IsSuccess );
		Assert.Equal(
			RegularExpressionDiagnosticCode.InvalidStartByteOffset,
			split.Diagnostic!.Code
		);
	}

	[Fact]
	public void PreparedTextInputRetainsUtf16Coordinates() {
		var expression = Compile( "." );
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>( expression );
		var prepared = PreparedRegexInput.Prepare( "x😀y" );

		var result = matcher.MatchPrepared(
			prepared,
			new RegularExpressionMatchOptions {
				StartIndex = 1,
				RequireMatchAtStart = true
			}
		);

		Assert.True( result.IsMatch );
		Assert.Equal( 1, result.Match!.Index );
		Assert.Equal( 2, result.Match.Length );
		Assert.Equal( "😀", result.Match.Value );
	}

	[Fact]
	public void PreparedInputAppliesMalformedUtf8ThrowPolicyDuringPreparation() {
		Assert.Throws<DecoderFallbackException>(
			() => PreparedRegexInput.Prepare(
				new byte[] { 0xff },
				new RegularExpressionInputOptions {
					DecodingMode = TextDecodingMode.Utf8,
					InvalidEncodingPolicy = InvalidEncodingPolicy.Throw
				}
			)
		);
	}

	[Fact]
	public void PreparedMatchingPreservesFiniteStateAccounting() {
		var expression = Compile(
			@"\(a\|aa\)*b",
			new RegularExpressionOptions { MaximumMatchStates = 10 }
		);
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>( expression );
		var prepared = PreparedRegexInput.Prepare( "aaaaaaaaaaaaaaaa" );

		var result = matcher.MatchPrepared(
			prepared,
			new RegularExpressionMatchOptions()
		);

		Assert.False( result.IsSuccess );
		Assert.Equal(
			RegularExpressionDiagnosticCode.MatchResourceLimitExceeded,
			result.Diagnostic!.Code
		);
	}

	private static ICompiledRegularExpression Compile(
		string pattern,
		RegularExpressionOptions? options = null
	) {
		var provider = new GnuBasicRegularExpressionProvider(
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var result = provider.Compile( pattern, options );
		Assert.True( result.IsSuccess, result.Diagnostic?.Message );
		return Assert.IsAssignableFrom<ICompiledRegularExpression>( result.Expression );
	}
}
