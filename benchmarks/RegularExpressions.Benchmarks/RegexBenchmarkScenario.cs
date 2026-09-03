namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

internal enum BenchmarkSyntax {
	Basic,
	Extended
}

internal enum BenchmarkInputProfile {
	Bytes,
	Utf8,
	Utf8PreserveInvalid,
	Utf8ReplaceInvalid
}

internal sealed record RegexBenchmarkScenario(
	string Name,
	BenchmarkSyntax Syntax,
	string Pattern,
	int InputLength,
	int MatchOffset,
	bool RequireMatchAtStart,
	BenchmarkInputProfile InputProfile,
	bool ExpectedMatch,
	int ExpectedMatchLength
) {
	internal RegularExpressionInputOptions CreateInputOptions() => this.InputProfile switch {
		BenchmarkInputProfile.Bytes => new() {
			DecodingMode = TextDecodingMode.Bytes
		},
		BenchmarkInputProfile.Utf8 => new() {
			DecodingMode = TextDecodingMode.Utf8,
			InvalidEncodingPolicy = InvalidEncodingPolicy.PreserveBytes
		},
		BenchmarkInputProfile.Utf8PreserveInvalid => new() {
			DecodingMode = TextDecodingMode.Utf8,
			InvalidEncodingPolicy = InvalidEncodingPolicy.PreserveBytes
		},
		BenchmarkInputProfile.Utf8ReplaceInvalid => new() {
			DecodingMode = TextDecodingMode.Utf8,
			InvalidEncodingPolicy = InvalidEncodingPolicy.Replace
		},
		_ => throw new InvalidOperationException(
			string.Concat(
				"Unknown benchmark input profile: ",
				this.InputProfile
			)
		)
	};

	internal RegularExpressionByteMatchOptions CreateMatchOptions() => new() {
		RequireMatchAtStart = this.RequireMatchAtStart
	};
}
