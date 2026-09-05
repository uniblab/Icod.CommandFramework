namespace Icod.CommandFramework.RegularExpressions;

/// <summary>Compiles GNU extended regular expressions with the Shared fully managed leftmost-longest engine.</summary>
public sealed class GnuExtendedRegularExpressionProvider : IRegularExpressionProvider {
	private readonly IRegularExpressionCharacterClassProvider characterClassProvider;

	/// <summary>Initializes a provider using current-culture Unicode classification and collation.</summary>
	public GnuExtendedRegularExpressionProvider() : this(
		UnicodeRegularExpressionCharacterClassProvider.CurrentCulture
	) {
	}

	/// <summary>Initializes a provider with an injectable character classification and collation policy.</summary>
	/// <param name="characterClassProvider">The character provider.</param>
	public GnuExtendedRegularExpressionProvider(
		IRegularExpressionCharacterClassProvider characterClassProvider
	) {
		ArgumentNullException.ThrowIfNull( characterClassProvider );
		this.characterClassProvider = characterClassProvider;
	}

	/// <summary>Gets a provider backed by the culture current when the property is read.</summary>
	public static GnuExtendedRegularExpressionProvider Default => new();

	/// <inheritdoc/>
	public RegularExpressionCompileResult Compile(
		string pattern,
		RegularExpressionOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( pattern );
		cancellationToken.ThrowIfCancellationRequested();
		var effective = ( options ?? new RegularExpressionOptions() ) with {
			Syntax = GnuRegularExpressionSyntax.Extended
		};
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero( effective.MaximumNestingDepth );
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero( effective.MaximumMatchStates );
		var parser = new GnuBasicRegularExpressionParser(
			pattern,
			effective,
			this.characterClassProvider,
			cancellationToken
		);
		var parseResult = parser.Parse();
		var expression = parseResult.Expression;
		if ( null == expression ) {
			return RegularExpressionCompileResult.Failed( parseResult.Diagnostic! );
		}
		ICompiledRegularExpression compiled = new GnuBasicCompiledRegularExpression(
			pattern,
			expression,
			parseResult.CaptureCount,
			effective,
			this.characterClassProvider
		);
		compiled = LiteralPrefixCompiledRegularExpression.Create(
			compiled,
			pattern,
			effective,
			this.characterClassProvider
		);
		compiled = new PreparedCompiledRegularExpression(
			compiled,
			pattern,
			expression,
			parseResult.CaptureCount,
			effective,
			this.characterClassProvider
		);
		return RegularExpressionCompileResult.Succeeded( compiled );
	}

	/// <inheritdoc/>
	public ValueTask<RegularExpressionCompileResult> CompileAsync(
		string pattern,
		RegularExpressionOptions? options = null,
		CancellationToken cancellationToken = default
	) => ValueTask.FromResult( this.Compile( pattern, options, cancellationToken ) );
}
