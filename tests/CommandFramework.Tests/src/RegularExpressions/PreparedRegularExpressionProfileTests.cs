namespace Icod.CommandFramework.Tests;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

using Xunit;

public sealed class PreparedRegularExpressionProfileTests {
	private static readonly RegularExpressionInputOptions ByteInputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};

	[Theory]
	[InlineData( GnuRegularExpressionSyntax.Basic )]
	[InlineData( GnuRegularExpressionSyntax.Extended )]
	[InlineData( GnuRegularExpressionSyntax.Emacs )]
	public void PreparedMatchingIsAvailableAcrossSyntaxProfiles(
		GnuRegularExpressionSyntax syntax
	) {
		var expression = Compile(
			syntax,
			"TARGET",
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>(
			expression
		);
		var prepared = PreparedRegexInput.Prepare(
			"xxTARGETyy"u8.ToArray(),
			ByteInputOptions
		);

		var result = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions()
		);

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 2, result.Match!.ByteIndex );
		Assert.Equal( "TARGET"u8.ToArray(), result.Match.Value.ToArray() );
	}

	[Fact]
	public void CancelledPreparedMatchDoesNotMutateReusableInput() {
		var expression = Compile(
			GnuRegularExpressionSyntax.Basic,
			"TARGET",
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>(
			expression
		);
		var prepared = PreparedRegexInput.Prepare(
			"xxTARGETyy"u8.ToArray(),
			ByteInputOptions
		);
		using var cancellationSource = new CancellationTokenSource();
		cancellationSource.Cancel();

		Assert.Throws<OperationCanceledException>(
			() => matcher.MatchPrepared(
				prepared,
				new RegularExpressionByteMatchOptions(),
				cancellationSource.Token
			)
		);

		var result = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions()
		);
		Assert.True( result.IsMatch );
		Assert.Equal( "TARGET"u8.ToArray(), result.Match!.Value.ToArray() );
	}

	[Fact]
	public void CancelledPreparationDoesNotPublishAPartialInput() {
		using var cancellationSource = new CancellationTokenSource();
		cancellationSource.Cancel();

		Assert.Throws<OperationCanceledException>(
			() => PreparedRegexInput.Prepare(
				"TARGET",
				cancellationSource.Token
			)
		);
		Assert.Throws<OperationCanceledException>(
			() => PreparedRegexInput.Prepare(
				"TARGET"u8.ToArray(),
				ByteInputOptions,
				cancellationSource.Token
			)
		);
	}

	[Theory]
	[InlineData( InvalidEncodingPolicy.PreserveBytes, false )]
	[InlineData( InvalidEncodingPolicy.Replace, true )]
	public void PreparedUtf8RetainsMalformedInputPolicy(
		InvalidEncodingPolicy invalidEncodingPolicy,
		bool expectedMatch
	) {
		var expression = Compile(
			GnuRegularExpressionSyntax.Basic,
			"�",
			UnicodeRegularExpressionCharacterClassProvider.InvariantCulture
		);
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>(
			expression
		);
		var source = new byte[] { 0xff };
		var inputOptions = new RegularExpressionInputOptions {
			DecodingMode = TextDecodingMode.Utf8,
			InvalidEncodingPolicy = invalidEncodingPolicy
		};
		var prepared = PreparedRegexInput.Prepare( source, inputOptions );

		var publicResult = expression.Match( source, inputOptions );
		var preparedResult = matcher.MatchPrepared(
			prepared,
			new RegularExpressionByteMatchOptions()
		);

		Assert.True( publicResult.IsSuccess );
		Assert.True( preparedResult.IsSuccess );
		Assert.Equal( expectedMatch, publicResult.IsMatch );
		Assert.Equal( expectedMatch, preparedResult.IsMatch );
		if ( expectedMatch ) {
			Assert.Equal( source, publicResult.Match!.Value.ToArray() );
			Assert.Equal( source, preparedResult.Match!.Value.ToArray() );
		}
	}

	[Fact]
	public void PreparedInputKindMismatchIsRejected() {
		var expression = Compile(
			GnuRegularExpressionSyntax.Basic,
			"TARGET",
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var matcher = Assert.IsAssignableFrom<IPreparedCompiledRegularExpression>(
			expression
		);
		var bytes = PreparedRegexInput.Prepare(
			"TARGET"u8.ToArray(),
			ByteInputOptions
		);
		var text = PreparedRegexInput.Prepare( "TARGET" );

		Assert.Throws<ArgumentException>(
			() => matcher.MatchPrepared(
				bytes,
				new RegularExpressionMatchOptions()
			)
		);
		Assert.Throws<ArgumentException>(
			() => matcher.MatchPrepared(
				text,
				new RegularExpressionByteMatchOptions()
			)
		);
	}

	private static ICompiledRegularExpression Compile(
		GnuRegularExpressionSyntax syntax,
		string pattern,
		IRegularExpressionCharacterClassProvider characterClassProvider
	) {
		IRegularExpressionProvider provider = syntax switch {
			GnuRegularExpressionSyntax.Basic => new GnuBasicRegularExpressionProvider(
				characterClassProvider
			),
			GnuRegularExpressionSyntax.Extended => new GnuExtendedRegularExpressionProvider(
				characterClassProvider
			),
			GnuRegularExpressionSyntax.Emacs => new GnuEmacsRegularExpressionProvider(
				characterClassProvider
			),
			_ => throw new ArgumentOutOfRangeException( nameof( syntax ) )
		};
		var result = provider.Compile( pattern );
		Assert.True( result.IsSuccess, result.Diagnostic?.Message );
		return Assert.IsAssignableFrom<ICompiledRegularExpression>( result.Expression );
	}
}
