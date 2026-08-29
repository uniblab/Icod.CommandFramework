namespace Icod.CommandFramework.FileSystem.Traversal;

/// <summary>
/// Configures collection-oriented pathname operand expansion.
/// </summary>
public sealed class PathnameOperandExpansionOptions {
	/// <summary>Gets a default immutable-by-convention option instance.</summary>
	public static PathnameOperandExpansionOptions Default { get; } = new();

	/// <summary>
	/// Gets or initializes the canonical low-level expansion options.
	/// </summary>
	public PathnameExpansionOptions ExpansionOptions { get; init; } =
		PathnameExpansionOptions.Default;

	/// <summary>
	/// Gets or initializes whether non-directory expanded matches are retained.
	/// Literal operands are always preserved.
	/// </summary>
	public bool IncludeFiles {
		get;
		init;
	} = true;

	/// <summary>
	/// Gets or initializes whether directory expanded matches are retained.
	/// Literal operands are always preserved.
	/// </summary>
	public bool IncludeDirectories {
		get;
		init;
	} = true;

	/// <summary>
	/// Gets or initializes how an expanded terminal pathname is dereferenced
	/// before file-versus-directory selection is applied.
	/// </summary>
	public PathDereferenceMode TerminalDereferenceMode {
		get;
		init;
	} = PathDereferenceMode.FollowEligiblePathIndirection;

	/// <summary>Validates the option values.</summary>
	/// <exception cref="ArgumentNullException">
	/// <see cref="ExpansionOptions"/> is <see langword="null"/>.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <see cref="TerminalDereferenceMode"/> is invalid.
	/// </exception>
	internal void Validate() {
		ArgumentNullException.ThrowIfNull(
			ExpansionOptions
		);
		ExpansionOptions.Validate();
		if (
			!Enum.IsDefined(
				typeof( PathDereferenceMode ),
				TerminalDereferenceMode
			)
		) {
			throw new ArgumentOutOfRangeException(
				nameof( TerminalDereferenceMode )
			);
		}
	}
}

/// <summary>
/// Represents a collected pathname-operand expansion result.
/// </summary>
public sealed class PathnameOperandExpansionResult {
	private readonly IReadOnlyList<string> operands;
	private readonly IReadOnlyList<PathnameExpansionEvent> issues;

	internal PathnameOperandExpansionResult(
		IEnumerable<string> operands,
		IEnumerable<PathnameExpansionEvent> issues
	) {
		ArgumentNullException.ThrowIfNull(
			operands
		);
		ArgumentNullException.ThrowIfNull(
			issues
		);
		this.operands = Array.AsReadOnly(
			operands.ToArray()
		);
		this.issues = Array.AsReadOnly(
			issues.ToArray()
		);
	}

	/// <summary>
	/// Gets the ordered expanded operand spellings suitable for command
	/// consumption. Relative inputs remain relative display paths.
	/// </summary>
	public IReadOnlyList<string> Operands {
		get {
			return operands;
		}
	}

	/// <summary>
	/// Gets non-root expansion events, including no-match, cycle, boundary,
	/// and structured error events.
	/// </summary>
	public IReadOnlyList<PathnameExpansionEvent> Issues {
		get {
			return issues;
		}
	}

	/// <summary>Gets whether any structured error event was collected.</summary>
	public bool HasErrors {
		get {
			return issues.Any(
				static item => item.Kind == PathnameExpansionEventKind.Error
			);
		}
	}
}
