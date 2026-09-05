namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using System.Text;
using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

internal static class BenchmarkSmoke {
	internal static int Run() {
		var names = new[] {
			"bre-one-char-4k-miss",
			"bre-literal-80-start",
			"bre-literal-4k-middle",
			"ere-literal-256k-miss",
			"bre-anchored-4k",
			"bre-utf8-anchored-4k",
			"bre-utf8-4k-middle",
			"bre-invalid-preserve-4k",
			"bre-invalid-replace-4k"
		}.Concat(
			RegexBenchmarkCatalog.Structural.Select( static scenario => scenario.Name )
		);

		foreach ( var name in names ) {
			var scenario = RegexBenchmarkCatalog.Get( name );
			var expression = RegexBenchmarkCatalog.Compile( scenario );
			var input = RegexBenchmarkCatalog.CreateInput( scenario );
			var result = expression.Match(
				input,
				scenario.CreateInputOptions(),
				scenario.CreateMatchOptions()
			);
			if ( !result.IsSuccess ) {
				throw new InvalidOperationException(
					string.Concat(
						"Benchmark smoke scenario failed: ",
						name,
						": ",
						result.Diagnostic?.Message
					)
				);
			}
			if ( scenario.ExpectedMatch != result.IsMatch ) {
				throw new InvalidOperationException(
					string.Concat(
						"Benchmark smoke match-state mismatch: ",
						name
					)
				);
			}
			if (
				scenario.ExpectedMatch
				&& null != result.Match
				&& scenario.ExpectedMatchLength != result.Match.ByteLength
			) {
				throw new InvalidOperationException(
					string.Concat(
						"Benchmark smoke match-length mismatch: ",
						name
					)
				);
			}
		}

		var provider = new GnuBasicRegularExpressionProvider(
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var compiled = provider.Compile( "." );
		if ( !compiled.IsSuccess || null == compiled.Expression ) {
			throw new InvalidOperationException( "Malformed-input smoke expression did not compile." );
		}
		try {
			_ = compiled.Expression.Match(
				new byte[] { 0xff },
				new RegularExpressionInputOptions {
					DecodingMode = TextDecodingMode.Utf8,
					InvalidEncodingPolicy = InvalidEncodingPolicy.Throw
				}
			);
			throw new InvalidOperationException(
				"Malformed UTF-8 Throw policy did not throw DecoderFallbackException."
			);
		} catch ( DecoderFallbackException ) {
		}

		Console.WriteLine( "R2.0 regex benchmark smoke passed." );
		return 0;
	}
}
