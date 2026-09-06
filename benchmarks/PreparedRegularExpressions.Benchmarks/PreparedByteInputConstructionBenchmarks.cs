namespace Icod.CommandFramework.RegularExpressions.PreparedBenchmarks;

using BenchmarkDotNet.Attributes;
using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

/// <summary>Measures construction cost for large immutable byte-mode prepared inputs.</summary>
[MemoryDiagnoser]
public class PreparedByteInputConstructionBenchmarks {
	private readonly byte[] input = Enumerable.Repeat(
		(byte)'x',
		1024 * 1024
	).ToArray();
	private readonly RegularExpressionInputOptions inputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};

	/// <summary>Prepares one 1 MiB authoritative byte-mode input.</summary>
	[Benchmark]
	public int PrepareByteMode1MiB() {
		return RegularExpressionPreparedByteInput.Prepare(
			this.input,
			this.inputOptions
		).Length;
	}
}
