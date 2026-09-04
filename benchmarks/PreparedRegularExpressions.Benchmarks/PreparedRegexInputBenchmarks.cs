namespace Icod.CommandFramework.RegularExpressions.PreparedBenchmarks;

using BenchmarkDotNet.Attributes;
using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

/// <summary>
/// Measures the reuse ceiling from matching one immutable prepared input repeatedly.
/// </summary>
[MemoryDiagnoser]
public class PreparedRegexInputBenchmarks {
	private readonly byte[] input;
	private readonly RegularExpressionInputOptions inputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};
	private readonly ICompiledRegularExpression expression;
	private readonly IPreparedCompiledRegularExpression preparedExpression;
	private readonly PreparedRegexInput preparedInput;

	/// <summary>
	/// Initializes the deterministic repeated-search comparison.
	/// </summary>
	public PreparedRegexInputBenchmarks() {
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
				compiled.Diagnostic?.Message ?? "Prepared-input benchmark expression did not compile."
			);
		}
		this.expression = compiled.Expression;
		this.preparedExpression = compiled.Expression as IPreparedCompiledRegularExpression
			?? throw new InvalidOperationException(
				"Compiled expression does not expose the internal prepared-input contract."
			);
		this.preparedInput = PreparedRegexInput.Prepare(
			this.input,
			this.inputOptions
		);
	}

	/// <summary>
	/// Enumerates matches through the unchanged public API, preparing the same input on every call.
	/// </summary>
	[Benchmark( Baseline = true )]
	public int PublicMatchLoop() {
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
					result.Diagnostic?.Message ?? "Public repeated-search benchmark failed."
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

	/// <summary>
	/// Enumerates matches while reusing one immutable prepared input.
	/// </summary>
	[Benchmark]
	public int PreparedMatchLoop() {
		var count = 0;
		var offset = 0;
		while ( offset <= this.input.Length ) {
			var result = this.preparedExpression.MatchPrepared(
				this.preparedInput,
				new RegularExpressionByteMatchOptions { StartByteOffset = offset }
			);
			if ( !result.IsSuccess ) {
				throw new InvalidOperationException(
					result.Diagnostic?.Message ?? "Prepared repeated-search benchmark failed."
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

	/// <summary>
	/// Verifies that both paths enumerate the same match count.
	/// </summary>
	internal static int RunSmoke() {
		var benchmark = new PreparedRegexInputBenchmarks();
		var publicCount = benchmark.PublicMatchLoop();
		var preparedCount = benchmark.PreparedMatchLoop();
		if ( publicCount != preparedCount || 0 == publicCount ) {
			Console.Error.WriteLine(
				string.Concat(
					"Prepared-input benchmark smoke mismatch: public=",
					publicCount,
					", prepared=",
					preparedCount,
					"."
				)
			);
			return 1;
		}
		return 0;
	}
}
