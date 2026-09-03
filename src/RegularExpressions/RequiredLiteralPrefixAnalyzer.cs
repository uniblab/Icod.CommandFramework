namespace Icod.CommandFramework.RegularExpressions;

using System.Buffers;
using System.Text;

/// <summary>
/// Derives a conservative leading literal run that every match must consume at its starting position.
/// </summary>
internal static class RequiredLiteralPrefixAnalyzer {
	/// <summary>
	/// Returns a leading literal run only when the source syntax proves that no alternate path can bypass it.
	/// </summary>
	internal static Rune[] Analyze( string pattern ) {
		ArgumentNullException.ThrowIfNull( pattern );
		if ( 0 == pattern.Length || ContainsAlternation( pattern ) ) {
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
			if ( OperationStatus.Done != status ) {
				return [];
			}

			if ( '\\' == value.Value ) {
				if ( index + consumed >= pattern.Length ) {
					return prefix.Count == 0 ? [] : [ .. prefix ];
				}
				var escaped = pattern[ index + consumed ];
				if ( escaped is '|' or '{' ) {
					return [];
				}
				return prefix.Count == 0 ? [] : [ .. prefix ];
			}

			if ( value.Value is '*' or '?' or '{' or '|' ) {
				return [];
			}
			if ( value.Value is '.' or '[' or '(' or ')' or '^' or '$' or '+' ) {
				break;
			}

			prefix.Add( value );
			index += consumed;
		}
		return [ .. prefix ];
	}

	private static bool ContainsAlternation( string pattern ) {
		for ( var index = 0; pattern.Length > index; index++ ) {
			if ( '|' == pattern[ index ] ) {
				return true;
			}
			if (
				'\\' == pattern[ index ]
				&& index + 1 < pattern.Length
				&& '|' == pattern[ index + 1 ]
			) {
				return true;
			}
		}
		return false;
	}
}
