namespace Icod.CommandFramework.RegularExpressions.PreparedBenchmarks;

using BenchmarkDotNet.Running;

internal static class Program {
	private static int Main( string[] args ) {
		ArgumentNullException.ThrowIfNull( args );
		if ( args.Contains( "--smoke", StringComparer.Ordinal ) ) {
			return PreparedRegexInputBenchmarks.RunSmoke();
		}

		BenchmarkSwitcher.FromAssembly( typeof( Program ).Assembly ).Run( args );
		return 0;
	}
}
