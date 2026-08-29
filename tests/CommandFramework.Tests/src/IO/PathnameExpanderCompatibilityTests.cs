using Icod.CommandFramework.IO;
using Xunit;

namespace Icod.CommandFramework.Tests.IO;

/// <summary>
/// Tests the legacy pathname-expansion facade over the canonical traversal
/// engine.
/// </summary>
public sealed class PathnameExpanderCompatibilityTests {
	/// <summary>
	/// Verifies that wildcard detection retains its historical public contract.
	/// </summary>
	[Fact]
	public void ContainsWildcardRecognizesOnlyStarAndQuestion() {
		Assert.True(
			PathnameExpander.ContainsWildcard(
				"*.txt"
			)
		);
		Assert.True(
			PathnameExpander.ContainsWildcard(
				"file?.txt"
			)
		);
		Assert.False(
			PathnameExpander.ContainsWildcard(
				"[a-z].txt"
			)
		);
		Assert.False(
			PathnameExpander.ContainsWildcard(
				"literal.txt"
			)
		);
	}

	/// <summary>
	/// Verifies ordinary and recursive expansion through the canonical engine.
	/// </summary>
	[Fact]
	public void ExpandsStarQuestionAndDoubleStarThroughCanonicalEngine() {
		using var workspace = new PathnameExpansionWorkspace();
		workspace.CreateFile(
			System.IO.Path.Combine(
				"root",
				"a.txt"
			)
		);
		workspace.CreateFile(
			System.IO.Path.Combine(
				"root",
				"nested",
				"b.txt"
			)
		);
		workspace.CreateFile(
			System.IO.Path.Combine(
				"root",
				"nested",
				"skip.bin"
			)
		);

		var result = PathnameExpander.Expand(
			new[] {
				System.IO.Path.Combine(
					"root",
					"**",
					"?.txt"
				)
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path
			}
		);

		Assert.Equal(
			new[] {
				System.IO.Path.Combine(
					"root",
					"a.txt"
				),
				System.IO.Path.Combine(
					"root",
					"nested",
					"b.txt"
				)
			},
			result
		);
	}

	/// <summary>
	/// Verifies literal, unmatched, and standard-input operands remain compatible.
	/// </summary>
	[Fact]
	public void PreservesLiteralUnmatchedAndStandardInputOperands() {
		using var workspace = new PathnameExpansionWorkspace();

		var preserved = PathnameExpander.Expand(
			new[] {
				"-",
				"literal.txt",
				"*.missing"
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path
			}
		);
		Assert.Equal(
			new[] {
				"-",
				"literal.txt",
				"*.missing"
			},
			preserved
		);

		var omitted = PathnameExpander.Expand(
			new[] {
				"*.missing"
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path,
				PreserveUnmatchedPatterns = false
			}
		);
		Assert.Empty(
			omitted
		);
	}

	/// <summary>
	/// Verifies legacy file and directory selection over canonical matches.
	/// </summary>
	[Fact]
	public void HonorsLegacyFileAndDirectorySelection() {
		using var workspace = new PathnameExpansionWorkspace();
		workspace.CreateFile(
			System.IO.Path.Combine(
				"root",
				"file.txt"
			)
		);
		workspace.CreateDirectory(
			System.IO.Path.Combine(
				"root",
				"directory"
			)
		);
		var pattern = System.IO.Path.Combine(
			"root",
			"*"
		);

		var files = PathnameExpander.Expand(
			new[] {
				pattern
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path
			}
		);
		Assert.Equal(
			new[] {
				System.IO.Path.Combine(
					"root",
					"file.txt"
				)
			},
			files
		);

		var directories = PathnameExpander.Expand(
			new[] {
				pattern
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path,
				IncludeFiles = false,
				IncludeDirectories = true
			}
		);
		Assert.Equal(
			new[] {
				System.IO.Path.Combine(
					"root",
					"directory"
				)
			},
			directories
		);

		var neither = PathnameExpander.Expand(
			new[] {
				"literal.txt",
				pattern
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path,
				IncludeFiles = false,
				IncludeDirectories = false
			}
		);
		Assert.Empty(
			neither
		);
	}

	/// <summary>
	/// Verifies that the facade retains legacy separator and leading-period behavior.
	/// </summary>
	[Fact]
	public void PreservesLegacySeparatorAndLeadingPeriodBehavior() {
		using var workspace = new PathnameExpansionWorkspace();
		workspace.CreateFile(
			System.IO.Path.Combine(
				"root",
				".hidden.txt"
			)
		);
		workspace.CreateFile(
			System.IO.Path.Combine(
				"root",
				"visible.txt"
			)
		);
		var alternatePattern = OperatingSystem.IsWindows()
			? "root/*.txt"
			: @"root\*.txt"
		;

		var result = PathnameExpander.Expand(
			new[] {
				alternatePattern
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path
			}
		);

		Assert.Contains(
			System.IO.Path.Combine(
				"root",
				".hidden.txt"
			),
			result
		);
		Assert.Contains(
			System.IO.Path.Combine(
				"root",
				"visible.txt"
			),
			result
		);
	}

	/// <summary>
	/// Verifies that bracket expressions remain literal through the legacy API.
	/// </summary>
	[Fact]
	public void TreatsBracketExpressionsAsLiteralLegacyText() {
		using var workspace = new PathnameExpansionWorkspace();
		workspace.CreateFile(
			"[a]-file.txt"
		);
		workspace.CreateFile(
			"a-file.txt"
		);

		var result = PathnameExpander.Expand(
			new[] {
				"[a]*.txt"
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path
			}
		);

		Assert.Equal(
			new[] {
				"[a]-file.txt"
			},
			result
		);
	}

	/// <summary>
	/// Verifies that recursive double-star expansion follows directory links
	/// only when the legacy option requests it.
	/// </summary>
	[Fact]
	public void RecursiveDoubleStarHonorsLegacyFollowDirectorySymlinksOption() {
		using var workspace = new PathnameExpansionWorkspace();
		workspace.CreateDirectory(
			"root"
		);
		workspace.CreateFile(
			System.IO.Path.Combine(
				"target",
				"inside.txt"
			)
		);
		if (
			!workspace.TryCreateDirectorySymbolicLink(
				System.IO.Path.Combine(
					"root",
					"link"
				),
				"target"
			)
		) {
			return;
		}

		var pattern = System.IO.Path.Combine(
			"root",
			"**",
			"inside.txt"
		);
		var withoutFollowing = PathnameExpander.Expand(
			new[] {
				pattern
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path,
				FollowDirectorySymlinks = false,
				PreserveUnmatchedPatterns = false
			}
		);
		Assert.Empty(
			withoutFollowing
		);

		var withFollowing = PathnameExpander.Expand(
			new[] {
				pattern
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path,
				FollowDirectorySymlinks = true,
				PreserveUnmatchedPatterns = false
			}
		);
		Assert.Equal(
			new[] {
				System.IO.Path.Combine(
					"root",
					"link",
					"inside.txt"
				)
			},
			withFollowing
		);
	}

	/// <summary>
	/// Verifies the historical behavior that finite wildcard expansion follows
	/// an intermediate directory link independently of the recursive-link flag.
	/// </summary>
	[Fact]
	public void FiniteWildcardPreservesHistoricalDirectorySymlinkTraversal() {
		using var workspace = new PathnameExpansionWorkspace();
		workspace.CreateDirectory(
			"root"
		);
		workspace.CreateFile(
			System.IO.Path.Combine(
				"target",
				"inside.txt"
			)
		);
		if (
			!workspace.TryCreateDirectorySymbolicLink(
				System.IO.Path.Combine(
					"root",
					"link"
				),
				"target"
			)
		) {
			return;
		}

		var result = PathnameExpander.Expand(
			new[] {
				System.IO.Path.Combine(
					"root",
					"l*",
					"inside.txt"
				)
			},
			new PathnameExpansionOptions {
				BaseDirectory = workspace.Path,
				FollowDirectorySymlinks = false,
				PreserveUnmatchedPatterns = false
			}
		);

		Assert.Equal(
			new[] {
				System.IO.Path.Combine(
					"root",
					"link",
					"inside.txt"
				)
			},
			result
		);
	}

	private sealed class PathnameExpansionWorkspace : IDisposable {
		internal PathnameExpansionWorkspace() {
			Path = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				"Icod.CommandFramework.Tests",
				Guid.NewGuid().ToString(
					"N"
				)
			);
			Directory.CreateDirectory(
				Path
			);
		}

		internal string Path {
			get;
		}

		internal void CreateDirectory(
			string relativePath
		) {
			ArgumentException.ThrowIfNullOrEmpty(
				relativePath
			);
			Directory.CreateDirectory(
				System.IO.Path.Combine(
					Path,
					relativePath
				)
			);
		}

		internal void CreateFile(
			string relativePath
		) {
			ArgumentException.ThrowIfNullOrEmpty(
				relativePath
			);
			var path = System.IO.Path.Combine(
				Path,
				relativePath
			);
			var directory = System.IO.Path.GetDirectoryName(
				path
			);
			if ( !string.IsNullOrEmpty( directory ) ) {
				Directory.CreateDirectory(
					directory
				);
			}
			File.WriteAllText(
				path,
				string.Empty
			);
		}

		internal bool TryCreateDirectorySymbolicLink(
			string linkRelativePath,
			string targetRelativePath
		) {
			ArgumentException.ThrowIfNullOrEmpty(
				linkRelativePath
			);
			ArgumentException.ThrowIfNullOrEmpty(
				targetRelativePath
			);
			var linkPath = System.IO.Path.Combine(
				Path,
				linkRelativePath
			);
			var targetPath = System.IO.Path.Combine(
				Path,
				targetRelativePath
			);
			var directory = System.IO.Path.GetDirectoryName(
				linkPath
			);
			if ( !string.IsNullOrEmpty( directory ) ) {
				Directory.CreateDirectory(
					directory
				);
			}
			try {
				Directory.CreateSymbolicLink(
					linkPath,
					targetPath
				);
				return true;
			} catch ( Exception exception ) when (
				exception is IOException
				or UnauthorizedAccessException
				or PlatformNotSupportedException
				or NotSupportedException
			) {
				return false;
			}
		}

		public void Dispose() {
			if ( Directory.Exists( Path ) ) {
				Directory.Delete(
					Path,
					recursive: true
				);
			}
		}
	}
}
