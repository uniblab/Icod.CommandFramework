using CanonicalLeadingPeriodPolicy =
	Icod.CommandFramework.FileSystem.Traversal.LeadingPeriodPolicy;
using CanonicalPathnameExpander =
	Icod.CommandFramework.FileSystem.Traversal.PathnameExpander;
using CanonicalPathnameExpansionMatchOrder =
	Icod.CommandFramework.FileSystem.Traversal.PathnameExpansionMatchOrder;
using CanonicalPathnameExpansionOptions =
	Icod.CommandFramework.FileSystem.Traversal.PathnameExpansionOptions;
using CanonicalPathnameOperandExpansionOptions =
	Icod.CommandFramework.FileSystem.Traversal.PathnameOperandExpansionOptions;
using CanonicalPathnamePatternOptions =
	Icod.CommandFramework.FileSystem.Traversal.PathnamePatternOptions;
using CanonicalSymbolicLinkTraversalMode =
	Icod.CommandFramework.FileSystem.Traversal.SymbolicLinkTraversalMode;
using CanonicalSystemReadOnlyFileSystemProvider =
	Icod.CommandFramework.FileSystem.Traversal.SystemReadOnlyFileSystemProvider;
using CanonicalUnmatchedPathnamePatternBehavior =
	Icod.CommandFramework.FileSystem.Traversal.UnmatchedPathnamePatternBehavior;

namespace Icod.CommandFramework.IO;

/// <summary>
/// Expands pathname operands containing <c>*</c>, <c>?</c>, and recursive
/// <c>**</c> path segments.
/// </summary>
/// <remarks>
/// This compatibility surface delegates wildcard traversal to the canonical
/// <see cref="Icod.CommandFramework.FileSystem.Traversal.PathnameExpander"/>
/// engine. Its historical public contract remains synchronous: a single
/// asterisk or question mark never crosses a directory separator, a segment
/// consisting of two asterisks matches zero or more directory levels, results
/// are deterministic, and unmatched patterns are preserved by default.
/// </remarks>
public static class PathnameExpander {

	/// <summary>
	/// Expands a sequence of pathname operands.
	/// </summary>
	public static IReadOnlyList<string> Expand(
		IEnumerable<string> operands,
		PathnameExpansionOptions? options = null
	) {
		ArgumentNullException.ThrowIfNull(
			operands
		);
		options ??= new PathnameExpansionOptions();
		if (
			!options.IncludeFiles
			&& !options.IncludeDirectories
		) {
			return Array.Empty<string>();
		}

		var expander = new CanonicalPathnameExpander(
			CanonicalSystemReadOnlyFileSystemProvider.Instance
		);
		var output = new List<string>();
		foreach ( var operand in operands ) {
			ArgumentNullException.ThrowIfNull(
				operand
			);
			if (
				"-" == operand
				|| !ContainsWildcard(
					operand
				)
			) {
				output.Add(
					operand
				);
				continue;
			}

			var matches = ExpandOne(
				expander,
				operand,
				options
			);
			if ( 0 == matches.Count ) {
				if ( options.PreserveUnmatchedPatterns ) {
					output.Add(
						operand
					);
				}
			} else {
				output.AddRange(
					matches
				);
			}
		}
		return output;
	}

	/// <summary>
	/// Returns whether a pathname contains a supported wildcard.
	/// </summary>
	public static bool ContainsWildcard(
		string value
	) {
		ArgumentNullException.ThrowIfNull(
			value
		);
		return value.IndexOfAny(
			new char[] {
				'*',
				'?'
			}
		) >= 0;
	}

	private static IReadOnlyList<string> ExpandOne(
		CanonicalPathnameExpander expander,
		string operand,
		PathnameExpansionOptions options
	) {
		ArgumentNullException.ThrowIfNull(
			expander
		);
		ArgumentNullException.ThrowIfNull(
			operand
		);
		ArgumentNullException.ThrowIfNull(
			options
		);

		var normalized = NormalizeSeparators(
			operand
		);
		var canonicalPattern = QuoteCharacterClasses(
			normalized
		);
		var expansionOptions = new CanonicalPathnameOperandExpansionOptions {
			ExpansionOptions = new CanonicalPathnameExpansionOptions {
				BaseDirectory = options.BaseDirectory,
				PatternOptions = new CanonicalPathnamePatternOptions {
					LeadingPeriodPolicy =
						CanonicalLeadingPeriodPolicy.WildcardMayMatch,
					BackslashEscapes = false
				},
				UnmatchedPatternBehavior =
					CanonicalUnmatchedPathnamePatternBehavior.ReturnNoMatches,
				MatchOrder = OperatingSystem.IsWindows()
					? CanonicalPathnameExpansionMatchOrder.OrdinalIgnoreCase
					: CanonicalPathnameExpansionMatchOrder.Ordinal,
				SymbolicLinkMode = ResolveSymbolicLinkMode(
					normalized,
					options
				)
			},
			IncludeFiles = options.IncludeFiles,
			IncludeDirectories = options.IncludeDirectories
		};

		var result = Task.Run(
			async () => await expander.ExpandOperandsAsync(
				new[] {
					canonicalPattern
				},
				expansionOptions
			).ConfigureAwait( false )
		).GetAwaiter().GetResult();

		return result.Operands;
	}

	private static CanonicalSymbolicLinkTraversalMode ResolveSymbolicLinkMode(
		string normalizedPattern,
		PathnameExpansionOptions options
	) {
		ArgumentNullException.ThrowIfNull(
			normalizedPattern
		);
		ArgumentNullException.ThrowIfNull(
			options
		);
		if ( options.FollowDirectorySymlinks ) {
			return CanonicalSymbolicLinkTraversalMode.Always;
		}
		return ContainsRecursiveSegment(
			normalizedPattern
		)
			? CanonicalSymbolicLinkTraversalMode.RootsOnly
			: CanonicalSymbolicLinkTraversalMode.Always
		;
	}

	private static bool ContainsRecursiveSegment(
		string value
	) {
		ArgumentNullException.ThrowIfNull(
			value
		);
		return value.Split(
			System.IO.Path.DirectorySeparatorChar,
			StringSplitOptions.RemoveEmptyEntries
		).Any(
			static segment => "**" == segment
		);
	}

	private static string QuoteCharacterClasses(
		string value
	) {
		ArgumentNullException.ThrowIfNull(
			value
		);
		var root = System.IO.Path.GetPathRoot(
			value
		) ?? string.Empty;
		var builder = new System.Text.StringBuilder(
			value.Length
		);
		builder.Append(
			root
		);
		for (
			var index = root.Length;
			index < value.Length;
			index++
		) {
			var character = value[ index ];
			switch ( character ) {
				case '[':
					builder.Append(
						"[[]"
					);
					break;
				case ']':
					builder.Append(
						"[]]"
					);
					break;
				default:
					builder.Append(
						character
					);
					break;
			}
		}
		return builder.ToString();
	}

	private static string NormalizeSeparators(
		string value
	) {
		ArgumentNullException.ThrowIfNull(
			value
		);
		if ( System.IO.Path.DirectorySeparatorChar == '\\' ) {
			return value.Replace(
				'/',
				'\\'
			);
		}
		return value.Replace(
			'\\',
			'/'
		);
	}

}
