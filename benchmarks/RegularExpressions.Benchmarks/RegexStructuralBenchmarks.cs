namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using BenchmarkDotNet.Attributes;
using Icod.CommandFramework.RegularExpressions;

/// <summary>Measures branching, repetition, assertion, capture, and backreference structures.</summary>
[MemoryDiagnoser]
public class RegexStructuralBenchmarks {
	private ICompiledRegularExpression? expression;
	private byte[] input = Array.Empty<byte>();
	private RegularExpressionInputOptions? inputOptions;
	private RegularExpressionByteMatchOptions? matchOptions;

	/// <summary>Gets the structural scenario.</summary>
	[ParamsSource( nameof( ScenarioNames ) )]
	public string ScenarioName { get; set; } = string.Empty;

	/// <summary>Gets the available structural scenarios.</summary>
	public IEnumerable<string> ScenarioNames => RegexBenchmarkCatalog.Structural.Select(
		static scenario => scenario.Name
	);

	/// <summary>Compiles the expression and creates deterministic input.</summary>
	[GlobalSetup]
	public void Setup() {
		var scenario = RegexBenchmarkCatalog.Get( this.ScenarioName );
		this.expression = RegexBenchmarkCatalog.Compile( scenario );
		this.input = RegexBenchmarkCatalog.CreateInput( scenario );
		this.inputOptions = scenario.CreateInputOptions();
		this.matchOptions = scenario.CreateMatchOptions();
	}

	/// <summary>Runs the structural expression.</summary>
	[Benchmark]
	public RegularExpressionByteMatchResult Match() => this.expression!.Match(
		this.input,
		this.inputOptions,
		this.matchOptions
	);
}
