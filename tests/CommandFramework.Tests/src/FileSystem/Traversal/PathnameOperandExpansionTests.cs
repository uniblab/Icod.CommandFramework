using Icod.CommandFramework.FileSystem.Traversal;
using Xunit;

namespace Icod.CommandFramework.Tests.FileSystem.Traversal;

/// <summary>
/// Tests collection-oriented expansion over the canonical pathname expander.
/// </summary>
public sealed class PathnameOperandExpansionTests {
	/// <summary>
	/// Verifies deterministic default ordering and relative display-path output.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task ExpandsOperandsInDeterministicDefaultOrder() {
		var basePath = CreateBasePath();
		var rootPath = System.IO.Path.Combine(
			basePath,
			"root"
		);
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory( basePath )
			.AddDirectory( rootPath )
			.AddFile(
				System.IO.Path.Combine(
					rootPath,
					"z.txt"
				)
			)
			.AddFile(
				System.IO.Path.Combine(
					rootPath,
					"a.txt"
				)
			);
		var expander = new PathnameExpander(
			provider
		);

		var result = await expander.ExpandOperandsAsync(
			new[] {
				System.IO.Path.Combine(
					"root",
					"*.txt"
				)
			},
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath,
					UnmatchedPatternBehavior =
						UnmatchedPathnamePatternBehavior.ReturnNoMatches
				}
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
					"z.txt"
				)
			},
			result.Operands
		);
		Assert.Empty(
			result.Issues
		);
		Assert.False(
			result.HasErrors
		);
	}

	/// <summary>
	/// Verifies that selection applies only to actual expanded matches.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task FiltersExpandedMatchesButPreservesLiteralOperands() {
		var basePath = CreateBasePath();
		var rootPath = System.IO.Path.Combine(
			basePath,
			"root"
		);
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory( basePath )
			.AddDirectory( rootPath )
			.AddFile(
				System.IO.Path.Combine(
					rootPath,
					"file.txt"
				)
			)
			.AddDirectory(
				System.IO.Path.Combine(
					rootPath,
					"directory"
				)
			);
		var expander = new PathnameExpander(
			provider
		);
		var operands = new[] {
			"literal.missing",
			System.IO.Path.Combine(
				"root",
				"*"
			)
		};

		var files = await expander.ExpandOperandsAsync(
			operands,
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath
				},
				IncludeDirectories = false
			}
		);
		Assert.Equal(
			new[] {
				"literal.missing",
				System.IO.Path.Combine(
					"root",
					"file.txt"
				)
			},
			files.Operands
		);

		var directories = await expander.ExpandOperandsAsync(
			operands,
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath
				},
				IncludeFiles = false
			}
		);
		Assert.Equal(
			new[] {
				"literal.missing",
				System.IO.Path.Combine(
					"root",
					"directory"
				)
			},
			directories.Operands
		);
	}

	/// <summary>
	/// Verifies preservation of the conventional standard-input operand.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task PreservesStandardInputOperand() {
		var basePath = CreateBasePath();
		var rootPath = System.IO.Path.Combine(
			basePath,
			"root"
		);
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory( basePath )
			.AddDirectory( rootPath )
			.AddFile(
				System.IO.Path.Combine(
					rootPath,
					"file.txt"
				)
			);
		var expander = new PathnameExpander(
			provider
		);

		var result = await expander.ExpandOperandsAsync(
			new[] {
				"-",
				System.IO.Path.Combine(
					"root",
					"*.txt"
				)
			},
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath
				}
			}
		);

		Assert.Equal(
			new[] {
				"-",
				System.IO.Path.Combine(
					"root",
					"file.txt"
				)
			},
			result.Operands
		);
	}

	/// <summary>
	/// Verifies that unmatched patterns are preserved by default.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task PreservesUnmatchedPatternByDefault() {
		var basePath = CreateBasePath();
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory( basePath );
		var expander = new PathnameExpander(
			provider
		);

		var result = await expander.ExpandOperandsAsync(
			new[] {
				"*.missing"
			},
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath
				}
			}
		);

		Assert.Equal(
			new[] {
				"*.missing"
			},
			result.Operands
		);
		Assert.Empty(
			result.Issues
		);
	}

	/// <summary>
	/// Verifies that non-root events remain available to command consumers.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task ReturnsNoMatchAndErrorIssuesWithoutInventingOperands() {
		var basePath = CreateBasePath();
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory( basePath );
		var expander = new PathnameExpander(
			provider
		);

		var noMatch = await expander.ExpandOperandsAsync(
			new[] {
				"*.missing"
			},
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath,
					UnmatchedPatternBehavior =
						UnmatchedPathnamePatternBehavior.ReturnNoMatches
				}
			}
		);
		Assert.Empty(
			noMatch.Operands
		);
		Assert.Equal(
			PathnameExpansionEventKind.NoMatch,
			Assert.Single(
				noMatch.Issues
			).Kind
		);
		Assert.False(
			noMatch.HasErrors
		);

		var error = await expander.ExpandOperandsAsync(
			new[] {
				"*.missing"
			},
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath,
					UnmatchedPatternBehavior =
						UnmatchedPathnamePatternBehavior.ReportError
				}
			}
		);
		Assert.Empty(
			error.Operands
		);
		Assert.Equal(
			PathTraversalErrorCode.NoPatternMatch,
			Assert.Single(
				error.Issues
			).Error!.Code
		);
		Assert.True(
			error.HasErrors
		);
	}

	private static string CreateBasePath() {
		return System.IO.Path.Combine(
			System.IO.Path.GetTempPath(),
			"Icod.CommandFramework.Tests",
			Guid.NewGuid().ToString(
				"N"
			)
		);
	}
}
