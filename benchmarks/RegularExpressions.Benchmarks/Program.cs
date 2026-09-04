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
		return ValidateBenchmarkArtifacts( ResolveArtifactDirectory( args ) );
	}

	private static string ResolveArtifactDirectory( string[] args ) {
		var artifactIndex = Array.IndexOf( args, "--artifacts" );
		if ( 0 > artifactIndex ) {
			artifactIndex = Array.IndexOf( args, "-a" );
		}
		if ( 0 > artifactIndex ) {
			return System.IO.Path.GetFullPath( "BenchmarkDotNet.Artifacts" );
		}
		if ( artifactIndex + 1 >= args.Length ) {
			return String.Empty;
		}
		return System.IO.Path.GetFullPath( args[ artifactIndex + 1 ] );
	}

	private static int ValidateBenchmarkArtifacts( string artifactDirectory ) {
		if ( String.IsNullOrWhiteSpace( artifactDirectory ) ) {
			Console.Error.WriteLine( "BenchmarkDotNet --artifacts requires an output path." );
			return 2;
		}
		if ( !Directory.Exists( artifactDirectory ) ) {
			Console.Error.WriteLine(
				$"BenchmarkDotNet did not produce its artifact directory: {artifactDirectory}"
			);
			return 1;
		}

		var logs = Directory.GetFiles(
			artifactDirectory,
			"*.log",
			SearchOption.AllDirectories
		);
		if ( 0 == logs.Length ) {
			Console.Error.WriteLine( "BenchmarkDotNet did not produce any log files." );
			return 1;
		}

		var executed = false;
		foreach ( var log in logs ) {
			var text = File.ReadAllText( log );
			foreach ( var marker in new[] {
				"BenchmarkDotNet has failed to build the auto-generated boilerplate code.",
				"Benchmarks with issues:",
				"// Build Error:",
				"Declaring type must be unsealed."
			} ) {
				if ( text.Contains( marker, StringComparison.Ordinal ) ) {
					Console.Error.WriteLine(
						$"BenchmarkDotNet failure marker '{marker}' was found in '{log}'."
					);
					return 1;
				}
			}

			foreach ( var line in File.ReadLines( log ) ) {
				const string Prefix = "Global total time:";
				if (
					!line.Contains( Prefix, StringComparison.Ordinal )
					|| !line.Contains( "executed benchmarks:", StringComparison.Ordinal )
				) {
					continue;
				}
				var markerIndex = line.LastIndexOf(
					"executed benchmarks:",
					StringComparison.Ordinal
				);
				var valueText = line[
					( markerIndex + "executed benchmarks:".Length )..
				].Trim();
				if (
					int.TryParse( valueText, out var count )
					&& 0 < count
				) {
					executed = true;
				}
			}
		}

		if ( !executed ) {
			Console.Error.WriteLine(
				"BenchmarkDotNet logs did not confirm that any benchmark executed."
			);
			return 1;
		}

		return 0;
	}
}
