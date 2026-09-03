namespace Icod.CommandFramework.Tests;

using System.Text;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

using Xunit;

public sealed class LiteralPrefixRegularExpressionTests {
	private static readonly IRegularExpressionCharacterClassProvider CharacterClasses =
		UnicodeRegularExpressionCharacterClassProvider.InvariantCulture;

	[Fact]
	public void BasicLiteralSearchSelectsTheLeftmostCandidate() {
		var expression = CompileBasic( "TARGET" );
		var result = expression.Match( "xxTARGETyyTARGET" );

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 2, result.Match!.Index );
		Assert.Equal( "TARGET", result.Match.Value );
	}

	[Fact]
	public void BasicLiteralSearchHonorsStartIndex() {
		var expression = CompileBasic( "TARGET" );
		var result = expression.Match(
			"xxTARGETyyTARGET",
			new RegularExpressionMatchOptions { StartIndex = 8 }
		);

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 10, result.Match!.Index );
	}

	[Fact]
	public void LiteralSearchHonorsIgnoreCaseProviderSemantics() {
		var expression = CompileBasic(
			"target",
			new RegularExpressionOptions { IgnoreCase = true }
		);
		var result = expression.Match( "xxTARGET" );

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 2, result.Match!.Index );
		Assert.Equal( "TARGET", result.Match.Value );
	}

	[Fact]
	public void ByteLiteralSearchSkipsOpaqueMalformedUnits() {
		var expression = CompileBasic( "TARGET" );
		var marker = Encoding.ASCII.GetBytes( "TARGET" );
		var input = new byte[ marker.Length + 1 ];
		input[ 0 ] = 0xff;
		marker.CopyTo( input, 1 );

		var result = expression.Match(
			input,
			new RegularExpressionInputOptions {
				DecodingMode = TextDecodingMode.Utf8,
				InvalidEncodingPolicy = InvalidEncodingPolicy.PreserveBytes
			}
		);

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 1, result.Match!.ByteIndex );
		Assert.Equal( marker.Length, result.Match.ByteLength );
	}

	[Fact]
	public void RequireMatchAtStartRetainsAnchoredBehavior() {
		var expression = CompileBasic( "TARGET" );
		var result = expression.Match(
			"xxTARGET",
			new RegularExpressionMatchOptions { RequireMatchAtStart = true }
		);

		Assert.True( result.IsSuccess );
		Assert.False( result.IsMatch );
	}

	[Fact]
	public void NonLiteralPatternFallsBackToGeneralMatcher() {
		var expression = CompileBasic( "TAR.*GET" );
		var result = expression.Match( "xxTAR-middle-GETyy" );

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( "TAR-middle-GET", result.Match!.Value );
	}

	[Fact]
	public void StructuredPrefixSearchContinuesAfterFalseCandidate() {
		var expression = CompileBasic( "TAR.*GET" );
		var result = expression.Match( "xxTAR-nope xxTAR-middle-GETyy" );

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 13, result.Match!.Index );
		Assert.Equal( "TAR-middle-GET", result.Match.Value );
	}

	[Fact]
	public void AlternationDoesNotAssumeFirstBranchLiteralPrefix() {
		var expression = Compile(
			GnuRegularExpressionSyntax.Extended,
			"TARGET|OTHER"
		);
		var result = expression.Match( "xxOTHERyy" );

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 2, result.Match!.Index );
		Assert.Equal( "OTHER", result.Match.Value );
	}

	[Fact]
	public void FiniteResourceLimitRetainsGeneralMatcherAccounting() {
		var expression = CompileBasic(
			"TARGET",
			new RegularExpressionOptions { MaximumMatchStates = 1 }
		);
		var result = expression.Match( "TARGET" );

		Assert.False( result.IsSuccess );
		Assert.Equal(
			RegularExpressionDiagnosticCode.MatchResourceLimitExceeded,
			result.Diagnostic!.Code
		);
	}

	[Theory]
	[InlineData( GnuRegularExpressionSyntax.Basic )]
	[InlineData( GnuRegularExpressionSyntax.Extended )]
	[InlineData( GnuRegularExpressionSyntax.Emacs )]
	public void PlainLiteralSearchIsEquivalentAcrossSyntaxProfiles(
		GnuRegularExpressionSyntax syntax
	) {
		var expression = Compile( syntax, "TARGET" );
		var result = expression.Match( "xxTARGETyy" );

		Assert.True( result.IsSuccess );
		Assert.True( result.IsMatch );
		Assert.Equal( 2, result.Match!.Index );
	}

	private static ICompiledRegularExpression CompileBasic(
		string pattern,
		RegularExpressionOptions? options = null
	) => Compile( GnuRegularExpressionSyntax.Basic, pattern, options );

	private static ICompiledRegularExpression Compile(
		GnuRegularExpressionSyntax syntax,
		string pattern,
		RegularExpressionOptions? options = null
	) {
		IRegularExpressionProvider provider = syntax switch {
			GnuRegularExpressionSyntax.Basic => new GnuBasicRegularExpressionProvider(
				CharacterClasses
			),
			GnuRegularExpressionSyntax.Extended => new GnuExtendedRegularExpressionProvider(
				CharacterClasses
			),
			GnuRegularExpressionSyntax.Emacs => new GnuEmacsRegularExpressionProvider(
				CharacterClasses
			),
			_ => throw new ArgumentOutOfRangeException( nameof( syntax ) )
		};
		var result = provider.Compile( pattern, options );
		Assert.True( result.IsSuccess );
		Assert.NotNull( result.Expression );
		return result.Expression;
	}
}
