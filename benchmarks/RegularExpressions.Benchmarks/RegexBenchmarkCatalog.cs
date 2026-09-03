namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using System.Text;
using Icod.CommandFramework.RegularExpressions;

internal static class RegexBenchmarkCatalog {
	private const string Target = "TARGET";
	private const string LongTarget = "TARGET-abcdefghijklmnopqrstuvwxyz-0123456789";

	private static readonly IReadOnlyList<RegexBenchmarkScenario> SearchScenarios = [
		new( "bre-one-char-4k-miss", BenchmarkSyntax.Basic, "Z", 4096, -1, false, BenchmarkInputProfile.Bytes, false, 0 ),
		new( "bre-literal-80-start", BenchmarkSyntax.Basic, Target, 80, 0, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-literal-4k-middle", BenchmarkSyntax.Basic, Target, 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-literal-256k-end", BenchmarkSyntax.Basic, Target, 262144, 262144 - Target.Length, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-literal-256k-miss", BenchmarkSyntax.Basic, Target, 262144, -1, false, BenchmarkInputProfile.Bytes, false, 0 ),
		new( "ere-literal-256k-miss", BenchmarkSyntax.Extended, Target, 262144, -1, false, BenchmarkInputProfile.Bytes, false, 0 ),
		new( "bre-long-literal-256k-miss", BenchmarkSyntax.Basic, LongTarget, 262144, -1, false, BenchmarkInputProfile.Bytes, false, 0 ),
		new( "bre-anchored-80", BenchmarkSyntax.Basic, Target, 80, 0, true, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-anchored-4k", BenchmarkSyntax.Basic, Target, 4096, 0, true, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-anchored-256k", BenchmarkSyntax.Basic, Target, 262144, 0, true, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-anchored-2m", BenchmarkSyntax.Basic, Target, 2097152, 0, true, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-utf8-anchored-4k", BenchmarkSyntax.Basic, Target, 4096, 0, true, BenchmarkInputProfile.Utf8, true, Target.Length ),
		new( "bre-utf8-anchored-256k", BenchmarkSyntax.Basic, Target, 262144, 0, true, BenchmarkInputProfile.Utf8, true, Target.Length ),
		new( "bre-utf8-4k-middle", BenchmarkSyntax.Basic, Target, 4096, 2048, false, BenchmarkInputProfile.Utf8, true, Target.Length ),
		new( "ere-utf8-256k-miss", BenchmarkSyntax.Extended, Target, 262144, -1, false, BenchmarkInputProfile.Utf8, false, 0 ),
		new( "bre-invalid-preserve-4k", BenchmarkSyntax.Basic, ".", 4096, 0, true, BenchmarkInputProfile.Utf8PreserveInvalid, true, 1 ),
		new( "bre-invalid-replace-4k", BenchmarkSyntax.Basic, ".", 4096, 0, true, BenchmarkInputProfile.Utf8ReplaceInvalid, true, 1 )
	];

	private static readonly IReadOnlyList<RegexBenchmarkScenario> StructuralScenarios = [
		new( "bre-alternation", BenchmarkSyntax.Basic, "\\(TARGET\\|OTHER\\)", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "ere-alternation", BenchmarkSyntax.Extended, "(TARGET|OTHER)", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "ere-repetition", BenchmarkSyntax.Extended, "TAR(GE)+T", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "ere-optional", BenchmarkSyntax.Extended, "TAR(GE)?T", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-bounded-repetition", BenchmarkSyntax.Basic, "TA\\{1,2\\}RGET", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-bracket-class", BenchmarkSyntax.Basic, "[[:upper:]]\\{6\\}", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-word-boundary", BenchmarkSyntax.Basic, "\\bTARGET\\b", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-capture-backreference", BenchmarkSyntax.Basic, "\\(ab\\)\\1", 4096, 2048, false, BenchmarkInputProfile.Bytes, true, 4 ),
		new( "ere-anchor", BenchmarkSyntax.Extended, "^TARGET$", Target.Length, 0, false, BenchmarkInputProfile.Bytes, true, Target.Length ),
		new( "bre-empty-match", BenchmarkSyntax.Basic, "a*", 80, -1, false, BenchmarkInputProfile.Bytes, true, 0 )
	];

	internal static IReadOnlyList<RegexBenchmarkScenario> Search => SearchScenarios;
	internal static IReadOnlyList<RegexBenchmarkScenario> Structural => StructuralScenarios;

	internal static RegexBenchmarkScenario Get( string name ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( name );
		foreach ( var scenario in SearchScenarios.Concat( StructuralScenarios ) ) {
			if ( string.Equals( scenario.Name, name, StringComparison.Ordinal ) ) {
				return scenario;
			}
		}
		throw new ArgumentException(
			string.Concat( "Unknown benchmark scenario: ", name ),
			nameof( name )
		);
	}

	internal static ICompiledRegularExpression Compile( RegexBenchmarkScenario scenario ) {
		ArgumentNullException.ThrowIfNull( scenario );
		IRegularExpressionProvider provider = scenario.Syntax switch {
			BenchmarkSyntax.Basic => new GnuBasicRegularExpressionProvider(
				PosixCLocaleRegularExpressionCharacterClassProvider.Instance
			),
			BenchmarkSyntax.Extended => new GnuExtendedRegularExpressionProvider(
				PosixCLocaleRegularExpressionCharacterClassProvider.Instance
			),
			_ => throw new InvalidOperationException(
				string.Concat( "Unknown benchmark syntax: ", scenario.Syntax )
			)
		};
		var result = provider.Compile( scenario.Pattern );
		if ( !result.IsSuccess || null == result.Expression ) {
			throw new InvalidOperationException(
				result.Diagnostic?.Message ?? "Benchmark regular expression did not compile."
			);
		}
		return result.Expression;
	}

	internal static byte[] CreateInput( RegexBenchmarkScenario scenario ) {
		ArgumentNullException.ThrowIfNull( scenario );
		if ( "bre-empty-match" == scenario.Name ) {
			return Encoding.ASCII.GetBytes( new string( 'b', scenario.InputLength ) );
		}
		if ( "bre-capture-backreference" == scenario.Name ) {
			return CreateEmbeddedInput( scenario.InputLength, scenario.MatchOffset, "abab" );
		}
		if ( "ere-anchor" == scenario.Name ) {
			return Encoding.ASCII.GetBytes( Target );
		}

		var token = scenario.Pattern.Contains( "abcdefghijklmnopqrstuvwxyz", StringComparison.Ordinal )
			? LongTarget
			: scenario.Pattern == "Z"
				? "Z"
				: Target;
		if (
			BenchmarkInputProfile.Utf8PreserveInvalid == scenario.InputProfile
			|| BenchmarkInputProfile.Utf8ReplaceInvalid == scenario.InputProfile
		) {
			var input = Enumerable.Repeat( (byte)'x', scenario.InputLength ).ToArray();
			input[ 0 ] = 0xff;
			return input;
		}
		if ( BenchmarkInputProfile.Utf8 == scenario.InputProfile ) {
			var input = CreateUtf8Input( scenario.InputLength );
			if ( 0 <= scenario.MatchOffset ) {
				var target = Encoding.ASCII.GetBytes( token );
				if ( 0 != scenario.MatchOffset % 2 ) {
					throw new InvalidOperationException(
						"UTF-8 benchmark marker offsets must align to the deterministic two-byte scalar boundary."
					);
				}
				target.CopyTo( input, scenario.MatchOffset );
			}
			return input;
		}
		return CreateEmbeddedInput(
			scenario.InputLength,
			scenario.MatchOffset,
			token
		);
	}

	private static byte[] CreateEmbeddedInput(
		int length,
		int matchOffset,
		string token
	) {
		var input = Enumerable.Repeat( (byte)'x', length ).ToArray();
		if ( 0 > matchOffset ) {
			return input;
		}
		var marker = Encoding.ASCII.GetBytes( token );
		if ( matchOffset + marker.Length > input.Length ) {
			throw new InvalidOperationException( "Benchmark marker does not fit inside the configured input." );
		}
		marker.CopyTo( input, matchOffset );
		if ( "TARGET" == token && 0 < matchOffset ) {
			input[ matchOffset - 1 ] = (byte)' ';
			if ( matchOffset + marker.Length < input.Length ) {
				input[ matchOffset + marker.Length ] = (byte)' ';
			}
		}
		return input;
	}

	private static byte[] CreateUtf8Input( int length ) {
		var input = new byte[ length ];
		var index = 0;
		while ( index + 1 < input.Length ) {
			input[ index ] = 0xc3;
			input[ index + 1 ] = 0xa9;
			index += 2;
		}
		if ( index < input.Length ) {
			input[ index ] = (byte)'x';
		}
		return input;
	}
}
