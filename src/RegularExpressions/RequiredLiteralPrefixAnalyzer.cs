namespace Icod.CommandFramework.RegularExpressions;

using System.Buffers;
using System.Text;

/// <summary>
/// Derives a conservative literal run that every match must consume at its starting position.
/// </summary>
internal static class RequiredLiteralPrefixAnalyzer {
	/// <summary>
	/// Returns the leading literal run that is syntactically guaranteed before the first
	/// potentially nonliteral regular-expression construct.
	/// </summary>
	internal static Rune[] Analyze( string pattern ) {
		ArgumentNullException.ThrowIfNull( pattern );
		if ( 0 == pattern.Length ) {
			return [];
		}

		var prefix = new List<Rune>( pattern.Length );
		var index = 0;
		while ( pattern.Length > index ) {
			var status = Rune.DecodeFromUtf16(
				pattern.AsSpan( index ),
				out var value,
				out var consumed
			);
			if ( OperationStatus.Done != status || IsPotentialOperator( value ) ) {
				break;
			}
			prefix.Add( value );
			index += consumed;
		}
		return [ .. prefix ];
	}

	private static bool IsPotentialOperator( Rune value ) => value.Value switch {
		'\\' or '.' or '[' or ']' or '*' or '^' or '$'
			or '(' or ')' or '{' or '}' or '?' or '+' or '|' => true,
		_ => false
	};
}
