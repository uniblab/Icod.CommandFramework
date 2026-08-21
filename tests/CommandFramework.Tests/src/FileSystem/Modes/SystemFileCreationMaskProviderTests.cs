namespace Icod.CommandFramework.Tests.FileSystem.Modes;

using Icod.CommandFramework.FileSystem.Modes;
using Xunit;

/// <summary>
/// Exercises command-neutral current-process creation-mask observation.
/// </summary>
public sealed class SystemFileCreationMaskProviderTests {
	/// <summary>Verifies the system provider always returns a bounded portable mask.</summary>
	[Fact]
	public void ReportsABoundedCreationMask() {
		var mask = SystemFileCreationMaskProvider.Instance.GetCurrentMask();

		Assert.InRange( mask.Value, 0, 0x01ff );
		if ( OperatingSystem.IsWindows() ) {
			Assert.Equal( FileCreationMask.None, mask );
		}
	}

	/// <summary>Verifies repeated observation restores any query-and-restore fallback state.</summary>
	[Fact]
	public void RepeatedObservationPreservesTheObservedMask() {
		var first = SystemFileCreationMaskProvider.Instance.GetCurrentMask();
		var second = SystemFileCreationMaskProvider.Instance.GetCurrentMask();

		Assert.Equal( first, second );
	}

	/// <summary>Verifies Linux prefers the non-mutating procfs observation when it is available.</summary>
	[Fact]
	public void LinuxObservationMatchesProcStatusWhenAvailable() {
		if ( !OperatingSystem.IsLinux() || !File.Exists( "/proc/self/status" ) ) {
			return;
		}

		string? value = null;
		foreach ( var line in File.ReadLines( "/proc/self/status" ) ) {
			if ( line.StartsWith( "Umask:", StringComparison.Ordinal ) ) {
				value = line[ "Umask:".Length.. ].Trim();
				break;
			}
		}
		if ( string.IsNullOrEmpty( value ) ) {
			return;
		}

		int expected;
		try {
			expected = Convert.ToInt32( value, 8 ) & 0x01ff;
		} catch ( FormatException ) {
			return;
		} catch ( OverflowException ) {
			return;
		}

		var actual = SystemFileCreationMaskProvider.Instance.GetCurrentMask();

		Assert.Equal( expected, actual.Value );
	}
}
