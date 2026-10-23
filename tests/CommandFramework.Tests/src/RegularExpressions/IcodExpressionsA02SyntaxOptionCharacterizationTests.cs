namespace Icod.CommandFramework.Tests;

using Icod.CommandFramework.RegularExpressions;
using Xunit;

/// <summary>
/// Observes independently selectable options and coupled GNU syntax behavior.
/// These are inherited outcomes, not claims of exact named Gnulib profiles.
/// </summary>
public sealed class IcodExpressionsA02SyntaxOptionCharacterizationTests {
	private static readonly GnuBasicRegularExpressionProvider Basic = new(
		PosixCLocaleRegularExpressionCharacterClassProvider.Instance
	);
	private static readonly GnuExtendedRegularExpressionProvider Extended = new(
		PosixCLocaleRegularExpressionCharacterClassProvider.Instance
	);

	[Theory]
	[InlineData( false, false, "\0", false )]
	[InlineData( true, false, "\0", true )]
	[InlineData( false, false, "\n", true )]
	[InlineData( true, false, "\n", true )]
	[InlineData( false, true, "\n", false )]
	[InlineData( true, true, "\n", false )]
	public void DotNullAndLinePoliciesHaveDistinctEffects(
		bool dotMatchesNull,
		bool newLineSensitive,
		string input,
		bool expectedMatch
	) {
		var compiled = Extended.Compile( ".", new() {
			DotMatchesNull = dotMatchesNull,
			NewLineSensitive = newLineSensitive
		} );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		Assert.Equal( expectedMatch, compiled.Expression!.Match( input ).IsMatch );
	}

	[Theory]
	[InlineData( false, false )]
	[InlineData( true, false )]
	[InlineData( false, true )]
	[InlineData( true, true )]
	public void NegatedBracketNewlinePolicyIsCoupledToLineSensitivity(
		bool dotMatchesNull,
		bool newLineSensitive
	) {
		var compiled = Extended.Compile( "[^x]", new() {
			DotMatchesNull = dotMatchesNull,
			NewLineSensitive = newLineSensitive
		} );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		Assert.Equal( !newLineSensitive, compiled.Expression!.Match( "\n" ).IsMatch );
	}

	[Theory]
	[InlineData( false )]
	[InlineData( true )]
	public void ReverseRangeAcceptanceCanBeSelectedInBothProviders( bool extended ) {
		var provider = extended ? (IRegularExpressionProvider)Extended : Basic;
		var rejected = provider.Compile( "[z-a]" );
		Assert.False( rejected.IsSuccess );
		Assert.Equal( RegularExpressionDiagnosticCode.InvalidRange, rejected.Diagnostic!.Code );

		var accepted = provider.Compile( "[z-a]", new() { AllowEmptyRanges = true } );
		Assert.True( accepted.IsSuccess, accepted.Diagnostic?.Message );
		Assert.False( accepted.Expression!.Match( "a" ).IsMatch );
	}

	[Theory]
	[InlineData( false )]
	[InlineData( true )]
	public void CaseFoldingCanBeSelectedInBothProviders( bool extended ) {
		var provider = extended ? (IRegularExpressionProvider)Extended : Basic;
		var sensitive = provider.Compile( "abc" );
		var insensitive = provider.Compile( "abc", new() { IgnoreCase = true } );
		Assert.True( sensitive.IsSuccess, sensitive.Diagnostic?.Message );
		Assert.True( insensitive.IsSuccess, insensitive.Diagnostic?.Message );
		Assert.False( sensitive.Expression!.Match( "ABC" ).IsMatch );
		Assert.Equal( "ABC", insensitive.Expression!.Match( "ABC" ).Match!.Value );
	}

	[Theory]
	[InlineData( false, @"\(a\)\1" )]
	[InlineData( true, @"(a)\1" )]
	public void NumericBackReferencesRemainEnabledInBothSyntaxBundles(
		bool extended,
		string pattern
	) {
		var compiled = extended ? Extended.Compile( pattern ) : Basic.Compile( pattern );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		var match = compiled.Expression!.Match( "aa", new() { RequireMatchAtStart = true } );
		Assert.True( match.IsMatch );
		Assert.Equal( "a", match.Match!.Captures[ 0 ].Value );
	}
}
