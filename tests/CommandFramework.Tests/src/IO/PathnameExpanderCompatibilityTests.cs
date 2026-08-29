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
