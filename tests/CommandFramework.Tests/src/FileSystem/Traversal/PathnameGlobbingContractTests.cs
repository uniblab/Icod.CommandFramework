using Icod.CommandFramework.FileSystem.Traversal;
using Xunit;

namespace Icod.CommandFramework.Tests.FileSystem.Traversal;

/// <summary>
/// Locks the public pathname-pattern and glob-expansion contract used by command
/// consumers.
/// </summary>
public sealed class PathnameGlobbingContractTests {
	/// <summary>
	/// Verifies that question mark consumes exactly one character.
	/// </summary>
	[Fact]
	public void QuestionMatchesExactlyOneCharacter() {
		Assert.True(
			PathnamePatternMatcher.IsSegmentMatch(
				"file?.txt",
				"file1.txt"
			)
		);
		Assert.False(
			PathnamePatternMatcher.IsSegmentMatch(
				"file?.txt",
				"file.txt"
			)
		);
		Assert.False(
			PathnamePatternMatcher.IsSegmentMatch(
				"file?.txt",
				"file12.txt"
			)
		);
	}

	/// <summary>
	/// Verifies ranges, negation, and literal malformed character classes.
	/// </summary>
	[Fact]
	public void CharacterClassesHaveDeterministicCanonicalSemantics() {
		Assert.True(
			PathnamePatternMatcher.IsSegmentMatch(
				"[a-c].txt",
				"b.txt"
			)
		);
		Assert.False(
			PathnamePatternMatcher.IsSegmentMatch(
				"[a-c].txt",
				"d.txt"
			)
		);
		Assert.True(
			PathnamePatternMatcher.IsSegmentMatch(
				"[!a-c].txt",
				"d.txt"
			)
		);
		Assert.True(
			PathnamePatternMatcher.IsSegmentMatch(
				"[^a-c].txt",
				"d.txt"
			)
		);
		Assert.True(
			PathnamePatternMatcher.IsSegmentMatch(
				"[abc",
				"[abc"
			)
		);
		Assert.False(
			PathnamePatternMatcher.IsSegmentMatch(
				"[abc",
				"abc"
			)
		);
	}

	/// <summary>
	/// Verifies that double-star recursion requires an entire pathname segment.
	/// </summary>
	[Fact]
	public void DoubleStarIsRecursiveOnlyAsWholeSegment() {
		var pattern = PathnamePattern.Parse(
			System.IO.Path.Combine(
				"root",
				"a**b.txt"
			)
		);

		Assert.True(
			pattern.IsMatch(
				System.IO.Path.Combine(
					"root",
					"axxxb.txt"
				)
			)
		);
		Assert.False(
			pattern.IsMatch(
				System.IO.Path.Combine(
					"root",
					"a",
					"x",
					"b.txt"
				)
			)
		);
	}

	/// <summary>
	/// Verifies that an explicitly named leading-period directory remains
	/// reachable after the zero-segment branch of a recursive wildcard.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task ExplicitHiddenSegmentCanFollowDoubleStar() {
		var basePath = CreateBasePath();
		var rootPath = System.IO.Path.Combine(
			basePath,
			"root"
		);
		var hiddenPath = System.IO.Path.Combine(
			rootPath,
			".hidden"
		);
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory(
				basePath
			)
			.AddDirectory(
				rootPath
			)
			.AddDirectory(
				hiddenPath
			)
			.AddFile(
				System.IO.Path.Combine(
					hiddenPath,
					"inside.txt"
				)
			);
		var expander = new PathnameExpander(
			provider
		);

		var events = await CollectAsync(
			expander.ExpandAsync(
				new[] {
					System.IO.Path.Combine(
						"root",
						"**",
						".hidden",
						"*.txt"
					)
				},
				new PathnameExpansionOptions {
					BaseDirectory = basePath,
					UnmatchedPatternBehavior =
						UnmatchedPathnamePatternBehavior.ReturnNoMatches
				}
			)
		);

		var root = Assert.Single(
			events
		).Root;
		Assert.NotNull(
			root
		);
		Assert.Equal(
			System.IO.Path.Combine(
				hiddenPath,
				"inside.txt"
			),
			root!.AccessPath
		);
	}

	/// <summary>
	/// Verifies that provider order remains available as an explicit opt-out
	/// from deterministic ordering.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task ProviderOrderCanBeRequestedExplicitly() {
		var basePath = CreateBasePath();
		var rootPath = System.IO.Path.Combine(
			basePath,
			"root"
		);
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory(
				basePath
			)
			.AddDirectory(
				rootPath
			)
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

		var events = await CollectAsync(
			expander.ExpandAsync(
				new[] {
					System.IO.Path.Combine(
						"root",
						"*.txt"
					)
				},
				new PathnameExpansionOptions {
					BaseDirectory = basePath,
					MatchOrder = PathnameExpansionMatchOrder.Provider,
					UnmatchedPatternBehavior =
						UnmatchedPathnamePatternBehavior.ReturnNoMatches
				}
			)
		);

		Assert.Equal(
			new[] {
				"z.txt",
				"a.txt"
			},
			events.Select(
				static item => System.IO.Path.GetFileName(
					item.Root!.AccessPath
				)
			)
		);
	}

	/// <summary>
	/// Verifies that collection filtering never deletes literal operands.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task CollectionFilteringPreservesLiteralOperands() {
		var basePath = CreateBasePath();
		var rootPath = System.IO.Path.Combine(
			basePath,
			"root"
		);
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory(
				basePath
			)
			.AddDirectory(
				rootPath
			)
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
				"literal.missing",
				System.IO.Path.Combine(
					"root",
					"*"
				)
			},
			new PathnameOperandExpansionOptions {
				ExpansionOptions = new PathnameExpansionOptions {
					BaseDirectory = basePath
				},
				IncludeFiles = false,
				IncludeDirectories = false
			}
		);

		Assert.Equal(
			new[] {
				"-",
				"literal.missing"
			},
			result.Operands
		);
	}

	/// <summary>
	/// Verifies that cancellation remains cancellation rather than becoming an
	/// expansion issue.
	/// </summary>
	/// <returns>A task representing the test.</returns>
	[Fact]
	public async Task CollectionExpansionPropagatesCancellation() {
		var basePath = CreateBasePath();
		var provider = new SyntheticReadOnlyFileSystemProvider()
			.AddDirectory(
				basePath
			);
		var expander = new PathnameExpander(
			provider
		);
		using var source = new CancellationTokenSource();
		source.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => expander.ExpandOperandsAsync(
				new[] {
					"*"
				},
				new PathnameOperandExpansionOptions {
					ExpansionOptions = new PathnameExpansionOptions {
						BaseDirectory = basePath
					}
				},
				source.Token
			).AsTask()
		);
	}

	private static async Task<PathnameExpansionEvent[]> CollectAsync(
		IAsyncEnumerable<PathnameExpansionEvent> source
	) {
		ArgumentNullException.ThrowIfNull(
			source
		);
		var events = new List<PathnameExpansionEvent>();
		await foreach ( var item in source ) {
			events.Add(
				item
			);
		}
		return events.ToArray();
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
