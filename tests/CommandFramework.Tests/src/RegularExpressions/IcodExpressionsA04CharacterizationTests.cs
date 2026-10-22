namespace Icod.CommandFramework.Tests;

using Icod.CommandFramework.RegularExpressions;
using Xunit;

/// <summary>
/// Pins in-situ GNU profile seams discovered by the Icod.Expressions A02/A03 audits.
/// These are observations of the current engine, not strict POSIX claims.
/// </summary>
public sealed class IcodExpressionsA04CharacterizationTests {
	private static readonly GnuBasicRegularExpressionProvider Basic = new(
		PosixCLocaleRegularExpressionCharacterClassProvider.Instance
	);
	private static readonly GnuExtendedRegularExpressionProvider Extended = new(
		PosixCLocaleRegularExpressionCharacterClassProvider.Instance
	);

	/// <summary>
	/// Pins the tac call shape: its Basic provider overrides an Emacs option bundle.
	/// </summary>
	[Fact]
	public void BasicProviderOverridesEmacsSyntaxRequestedByTac() {
		var compiled = Basic.Compile( "ab+", RegularExpressionOptions.GnuEmacsCompatibility );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );

		var literalPlus = compiled.Expression!.Match( "ab+", new() { RequireMatchAtStart = true } );
		Assert.True( literalPlus.IsMatch );
		Assert.Equal( "ab+", literalPlus.Match!.Value );

		var repetition = compiled.Expression.Match( "abbb", new() { RequireMatchAtStart = true } );
		Assert.False( repetition.IsMatch );
	}

	/// <summary>
	/// Pins that a newline in the pattern is a literal, not Gnulib RE_NEWLINE_ALT.
	/// </summary>
	[Theory]
	[InlineData( false )]
	[InlineData( true )]
	public void PatternNewlineRemainsLiteralInCurrentBasicAndExtendedProviders( bool extended ) {
		var compiled = extended ? Extended.Compile( "a\nb" ) : Basic.Compile( "a\nb" );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		Assert.False( compiled.Expression!.Match( "ab" ).IsMatch );
		var literal = compiled.Expression.Match( "a\nb" );
		Assert.True( literal.IsMatch );
		Assert.Equal( "a\nb", literal.Match!.Value );
	}

	/// <summary>
	/// Pins the current context-sensitive anchors rather than implying the
	/// RE_CONTEXT_INDEP_ANCHORS bit is independently implemented.
	/// </summary>
	[Theory]
	[InlineData( false )]
	[InlineData( true )]
	public void MidBranchCaretAndDollarRemainLiterals( bool extended ) {
		var caret = extended ? Extended.Compile( "a^b" ) : Basic.Compile( "a^b" );
		var dollar = extended ? Extended.Compile( "a$b" ) : Basic.Compile( "a$b" );
		Assert.True( caret.IsSuccess, caret.Diagnostic?.Message );
		Assert.True( dollar.IsSuccess, dollar.Diagnostic?.Message );
		Assert.Equal( "a^b", caret.Expression!.Match( "a^b" ).Match!.Value );
		Assert.Equal( "a$b", dollar.Expression!.Match( "a$b" ).Match!.Value );
	}

	/// <summary>
	/// Characterizes capture selection for an empty anchor branch. This differs
	/// from a related pinned Gnulib test-regex.c expectation; do not alter it
	/// during mechanical extraction.
	/// </summary>
	[Theory]
	[InlineData( "(^|x)z" )]
	[InlineData( "(x|^)z" )]
	public void EmptyAnchorBranchCurrentlyParticipatesAsAnEmptyCapture( string pattern ) {
		var compiled = Extended.Compile( pattern );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		var result = compiled.Expression!.Match( "z" );
		Assert.True( result.IsMatch );
		Assert.Equal( "z", result.Match!.Value );
		Assert.True( result.Match.Captures[ 0 ].Success );
		Assert.Equal( 0, result.Match.Captures[ 0 ].Index );
		Assert.Equal( 0, result.Match.Captures[ 0 ].Length );
		Assert.Equal( "", result.Match.Captures[ 0 ].Value );
	}
}
