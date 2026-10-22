namespace Icod.CommandFramework.Tests;

using System.Text;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;
using Xunit;

/// <summary>
/// Additional pre-extraction observations shaped like audited consumers.
/// These tests supplement, rather than rewrite, the first 193-case checkpoint.
/// Search progression and output formatting remain the consumers' policies.
/// </summary>
public sealed class IcodExpressionsA04ConsumerShapeTests {
	private static readonly GnuExtendedRegularExpressionProvider Extended = new(
		PosixCLocaleRegularExpressionCharacterClassProvider.Instance
	);

	[Fact]
	public void GrepPreparedUtf8SearchPreservesByteOffsetsAcrossRepeatedCalls() {
		var compiled = Extended.Compile( "(a+)", RegularExpressionOptions.GnuExtendedCompatibility );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			Encoding.UTF8.GetBytes( "éaa a" ),
			new RegularExpressionInputOptions { DecodingMode = TextDecodingMode.Utf8 }
		);
		var expression = compiled.Expression!;
		var first = expression.Match( prepared, new() { StartByteOffset = 2 } );
		var second = expression.Match( prepared, new() { StartByteOffset = 4 } );
		Assert.True( first.IsSuccess );
		Assert.True( second.IsSuccess );
		Assert.Equal( 2, first.Match!.ByteIndex );
		Assert.Equal( 2, first.Match.ByteLength );
		Assert.Equal( 2, first.Match.Captures[ 0 ].ByteIndex );
		Assert.Equal( "aa"u8.ToArray(), first.Match.Captures[ 0 ].Value.ToArray() );
		Assert.Equal( 5, second.Match!.ByteIndex );
		Assert.Equal( 1, second.Match.ByteLength );
	}

	[Fact]
	public void ProcPsStringSearchDistinguishesSearchFromAnchoredOffset() {
		var compiled = Extended.Compile( "(cat|dog)", RegularExpressionOptions.GnuExtendedCompatibility );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		var expression = compiled.Expression!;
		var search = expression.Match( "xxdog" );
		var refused = expression.Match( "xxdog", new() { RequireMatchAtStart = true } );
		var anchored = expression.Match( "xxdog", new() { StartIndex = 2, RequireMatchAtStart = true } );
		Assert.Equal( 2, search.Match!.Index );
		Assert.Equal( "dog", search.Match.Captures[ 0 ].Value );
		Assert.False( refused.IsMatch );
		Assert.Equal( 2, anchored.Match!.Index );
	}

	[Fact]
	public void PtxBoundedAnchoredSearchOnlySeesCallerSuppliedPrefix() {
		var provider = new GnuEmacsRegularExpressionProvider(
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var compiled = provider.Compile( "a+", RegularExpressionOptions.GnuEmacsCompatibility );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		var options = new RegularExpressionMatchOptions { RequireMatchAtStart = true };
		var expression = compiled.Expression!;
		var bounded = expression.Match( "aa", options );
		var whole = expression.Match( "aaab", options );
		Assert.Equal( "aa", bounded.Match!.Value );
		Assert.Equal( "aaa", whole.Match!.Value );
	}

	[Fact]
	public void SedNullSeparatedAnchorsHonorSubsequentSearchStart() {
		var compiled = Extended.Compile( "^b$", new RegularExpressionOptions {
			NewLineSensitive = true,
			LineSeparator = new Rune( '\0' ),
			DotMatchesNull = true
		} );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		var expression = compiled.Expression!;
		var first = expression.Match( "a\0b\0c\0b" );
		var second = expression.Match( "a\0b\0c\0b", new() { StartIndex = 3 } );
		Assert.Equal( 2, first.Match!.Index );
		Assert.Equal( 6, second.Match!.Index );
	}

	[Fact]
	public void EdEmptyMatchCanBeFollowedByCallerAdvancedSearch() {
		var provider = new GnuBasicRegularExpressionProvider(
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var compiled = provider.Compile( "a*" );
		Assert.True( compiled.IsSuccess, compiled.Diagnostic?.Message );
		var expression = compiled.Expression!;
		var first = expression.Match( "ba" );
		var advanced = expression.Match( "ba", new() { StartIndex = 1 } );
		Assert.Equal( 0, first.Match!.Index );
		Assert.Equal( 0, first.Match.Length );
		Assert.Equal( 1, advanced.Match!.Index );
		Assert.Equal( "a", advanced.Match.Value );
	}
}
