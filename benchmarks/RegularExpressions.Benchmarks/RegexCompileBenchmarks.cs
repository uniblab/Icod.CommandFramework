namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using BenchmarkDotNet.Attributes;
using Icod.CommandFramework.RegularExpressions;

/// <summary>Measures compilation cost independently from matching.</summary>
[MemoryDiagnoser]
public class RegexCompileBenchmarks {
	private static readonly GnuBasicRegularExpressionProvider Basic = new(
		PosixCLocaleRegularExpressionCharacterClassProvider.Instance
	);
	private static readonly GnuExtendedRegularExpressionProvider Extended = new(
		PosixCLocaleRegularExpressionCharacterClassProvider.Instance
	);

	/// <summary>Gets the compilation workload.</summary>
	[Params( "bre-literal", "ere-alternation", "bre-backreference", "ere-repetition" )]
	public string ScenarioName { get; set; } = string.Empty;

	/// <summary>Compiles one representative regular expression.</summary>
	[Benchmark]
	public RegularExpressionCompileResult Compile() => this.ScenarioName switch {
		"bre-literal" => Basic.Compile( "TARGET" ),
		"ere-alternation" => Extended.Compile( "(TARGET|OTHER)" ),
		"bre-backreference" => Basic.Compile( "\\(ab\\)\\1" ),
		"ere-repetition" => Extended.Compile( "TAR(GE|XX){1,3}T" ),
		_ => throw new InvalidOperationException(
			string.Concat( "Unknown compile benchmark scenario: ", this.ScenarioName )
		)
	};
}
