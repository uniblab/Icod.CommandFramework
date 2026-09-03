namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using BenchmarkDotNet.Attributes;
using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

/// <summary>Measures repeated searches over the same record, as used by only-matching consumers.</summary>
[MemoryDiagnoser]
public class RegexRepeatedSearchBenchmarks {
	private readonly byte[] input;
	private readonly ICompiledRegularExpression expression;
	private readonly RegularExpressionInputOptions inputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};

	/// <summary>Initializes the deterministic repeated-search workload.</summary>
	public RegexRepeatedSearchBenchmarks() {
		this.input = Enumerable.Repeat( (byte)'x', 65536 ).ToArray();
		var marker = "TARGET"u8.ToArray();
		for ( var offset = 512; offset + marker.Length < this.input.Length; offset += 1024 ) {
			marker.CopyTo( this.input, offset );
		}

		var provider = new GnuBasicRegularExpressionProvider(
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var compiled = provider.Compile( "TARGET" );
		if ( !compiled.IsSuccess || null == compiled.Expression ) {
			throw new InvalidOperationException(
				compiled.Diagnostic?.Message ?? "Repeated-search benchmark expression did not compile."
			);
		}
		this.expression = compiled.Expression;
	}

	/// <summary>Enumerates all non-overlapping literal matches by repeated public Match calls.</summary>
	[Benchmark]
	public int FindAll() {
		var count = 0;
		var offset = 0;
		while ( offset <= this.input.Length ) {
			var result = this.expression.Match(
				this.input,
				this.inputOptions,
				new RegularExpressionByteMatchOptions { StartByteOffset = offset }
			);
			if ( !result.IsSuccess ) {
				throw new InvalidOperationException(
					result.Diagnostic?.Message ?? "Repeated-search benchmark failed."
				);
			}
			if ( null == result.Match ) {
				break;
			}
			count++;
			var next = result.Match.ByteIndex + Math.Max( 1, result.Match.ByteLength );
			if ( next <= offset ) {
				break;
			}
			offset = next;
		}
		return count;
	}
}
