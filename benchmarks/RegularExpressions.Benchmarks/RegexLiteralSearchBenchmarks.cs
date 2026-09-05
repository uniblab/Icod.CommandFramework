namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using BenchmarkDotNet.Attributes;
using Icod.CommandFramework.RegularExpressions;

/// <summary>Measures unanchored literal search across syntax, input size, and hit position.</summary>
[MemoryDiagnoser]
public class RegexLiteralSearchBenchmarks {
	private ICompiledRegularExpression? expression;
	private byte[] input = Array.Empty<byte>();
	private RegularExpressionInputOptions? inputOptions;
	private RegularExpressionByteMatchOptions? matchOptions;

	/// <summary>Gets the direct-search scenario.</summary>
	[ParamsSource( nameof( ScenarioNames ) )]
	public string ScenarioName { get; set; } = string.Empty;

	/// <summary>Gets the available direct-search scenarios.</summary>
	public IEnumerable<string> ScenarioNames => RegexBenchmarkCatalog.Search
		.Where( static scenario => !scenario.RequireMatchAtStart )
		.Where( static scenario => !scenario.Name.Contains( "invalid", StringComparison.Ordinal ) )
		.Select( static scenario => scenario.Name );

	/// <summary>Compiles the expression and creates deterministic input.</summary>
	[GlobalSetup]
	public void Setup() {
		var scenario = RegexBenchmarkCatalog.Get( this.ScenarioName );
		this.expression = RegexBenchmarkCatalog.Compile( scenario );
		this.input = RegexBenchmarkCatalog.CreateInput( scenario );
		this.inputOptions = scenario.CreateInputOptions();
		this.matchOptions = scenario.CreateMatchOptions();
	}

	/// <summary>Searches the authoritative byte input.</summary>
	[Benchmark]
	public RegularExpressionByteMatchResult Match() => this.expression!.Match(
		this.input,
		this.inputOptions,
		this.matchOptions
	);
}
