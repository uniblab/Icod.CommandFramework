namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using BenchmarkDotNet.Attributes;
using Icod.CommandFramework.RegularExpressions;

/// <summary>Measures full-input decode/setup cost with a single required start position.</summary>
[MemoryDiagnoser]
public class RegexDecodeBenchmarks {
	private ICompiledRegularExpression? expression;
	private byte[] input = Array.Empty<byte>();
	private RegularExpressionInputOptions? inputOptions;
	private RegularExpressionByteMatchOptions? matchOptions;

	/// <summary>Gets the decode/setup scenario.</summary>
	[ParamsSource( nameof( ScenarioNames ) )]
	public string ScenarioName { get; set; } = string.Empty;

	/// <summary>Gets the available decode/setup scenarios.</summary>
	public IEnumerable<string> ScenarioNames => RegexBenchmarkCatalog.Search
		.Where( static scenario => scenario.RequireMatchAtStart )
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

	/// <summary>Matches only at the configured input start.</summary>
	[Benchmark]
	public RegularExpressionByteMatchResult MatchAtStart() => this.expression!.Match(
		this.input,
		this.inputOptions,
		this.matchOptions
	);
}
