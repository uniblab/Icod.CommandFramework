namespace Icod.CommandFramework.RegularExpressions.Benchmarks;

using BenchmarkDotNet.Running;

internal static class Program {
	private static int Main( string[] args ) {
		ArgumentNullException.ThrowIfNull( args );
		if ( args.Contains( "--smoke", StringComparer.Ordinal ) ) {
			return BenchmarkSmoke.Run();
		}
		var metadataIndex = Array.IndexOf( args, "--metadata" );
		if ( 0 <= metadataIndex ) {
			if ( metadataIndex + 1 >= args.Length ) {
				Console.Error.WriteLine( "--metadata requires an output path." );
				return 2;
			}
			BenchmarkMetadata.Write( args[ metadataIndex + 1 ] );
			return 0;
		}

		BenchmarkMetadata.WriteRequestedMetadata();
		BenchmarkSwitcher.FromAssembly( typeof( Program ).Assembly ).Run( args );
		return 0;
	}
}
