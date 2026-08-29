namespace Icod.CommandFramework.FileSystem.Traversal;

public sealed partial class PathnameExpander {
	/// <summary>
	/// Expands pathname operands into the ordered display-path collection most
	/// command implementations consume while retaining non-root events
	/// separately.
	/// </summary>
	/// <param name="operands">The pathname operands.</param>
	/// <param name="options">The collection-oriented expansion options.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The collected operands and non-root expansion events.</returns>
	/// <remarks>
	/// Literal operands are passed through by their display spelling and are
	/// not subject to file-versus-directory filtering. Consequently the
	/// conventional standard-input operand <c>-</c> remains <c>-</c>, and an
	/// unmatched pattern preserved as a literal remains unchanged.
	/// </remarks>
	public async ValueTask<PathnameOperandExpansionResult> ExpandOperandsAsync(
		IEnumerable<string> operands,
		PathnameOperandExpansionOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull(
			operands
		);
		options ??= PathnameOperandExpansionOptions.Default;
		options.Validate();

		var expandedOperands = new List<string>();
		var issues = new List<PathnameExpansionEvent>();
		await foreach (
			var item in ExpandAsync(
				operands,
				options.ExpansionOptions,
				cancellationToken
			)
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( item.Root is not PathTraversalRoot root ) {
				issues.Add(
					item
				);
				continue;
			}

			if ( root.Kind == PathTraversalRootKind.Literal ) {
				expandedOperands.Add(
					root.DisplayPath
				);
				continue;
			}
			if (
				options.IncludeFiles
				&& options.IncludeDirectories
			) {
				expandedOperands.Add(
					root.DisplayPath
				);
				continue;
			}

			ReadOnlyFileSystemEntry observation;
			try {
				observation = await _provider.ObserveAsync(
					root.AccessPath,
					options.TerminalDereferenceMode,
					cancellationToken
				).ConfigureAwait( false );
			} catch ( OperationCanceledException ) when (
				cancellationToken.IsCancellationRequested
			) {
				throw;
			} catch ( Exception exception ) {
				var issue = CreateOperandSelectionError(
					root,
					"The expanded pathname could not be observed for file-versus-directory selection.",
					exception
				);
				issues.Add(
					issue
				);
				if (
					options.ExpansionOptions.ErrorMode
						== PathTraversalErrorMode.Stop
				) {
					break;
				}
				continue;
			}

			if ( observation.Kind == FileSystemEntryKind.Unknown ) {
				var issue = CreateOperandSelectionError(
					root,
					"The expanded pathname could not be classified for file-versus-directory selection."
				);
				issues.Add(
					issue
				);
				if (
					options.ExpansionOptions.ErrorMode
						== PathTraversalErrorMode.Stop
				) {
					break;
				}
				continue;
			}

			var isDirectory = observation.Kind == FileSystemEntryKind.Directory;
			if (
				(
					isDirectory
					&& options.IncludeDirectories
				)
				|| (
					!isDirectory
					&& options.IncludeFiles
				)
			) {
				expandedOperands.Add(
					root.DisplayPath
				);
			}
		}

		return new PathnameOperandExpansionResult(
			expandedOperands,
			issues
		);
	}

	private static PathnameExpansionEvent CreateOperandSelectionError(
		PathTraversalRoot root,
		string message,
		Exception? exception = null
	) {
		ArgumentNullException.ThrowIfNull(
			root
		);
		ArgumentException.ThrowIfNullOrEmpty(
			message
		);
		return PathnameExpansionEvent.CreateError(
			root.OriginalOperand,
			root.OperandIndex,
			new PathTraversalError(
				PathTraversalErrorCode.ObservationFailed,
				root,
				root.AccessPath,
				PathTraversalOperationStage.ObserveEntry,
				PathTraversalErrorScope.Entry,
				message,
				exception
			)
		);
	}
}
