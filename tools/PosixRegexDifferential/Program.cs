using System.Runtime.InteropServices;
using Icod.CommandFramework.RegularExpressions;

// Linux/glibc, ASCII, C locale only. This independent implementation is an
// observation, not an executable copy of POSIX Issue 8 or pinned Gnulib.
if ( !OperatingSystem.IsLinux() ) {
	Console.Error.WriteLine( "This differential requires Linux/glibc." );
	return 2;
}
if ( Native.setlocale( 6, "C" ) == IntPtr.Zero ) {
	Console.Error.WriteLine( "Unable to select the C locale." );
	return 2;
}
Console.WriteLine( $"glibc {Marshal.PtrToStringAnsi( Native.gnu_get_libc_version() )}; locale C" );

var cases = new FixtureCase[] {
	new( "bre-interval-longest", false, @"a\{2,3\}", "zaaa", [S( 1, 4 )], [S( 1, 4 )], "defined ASCII" ),
	new( "bre-backreference", false, @"\(ab\)\1", "xabab", [S( 1, 5 ), S( 1, 3 )], [S( 1, 5 ), S( 1, 3 )], "defined ASCII" ),
	new( "bre-empty-capture", false, @"\(a*\)b", "b", [S( 0, 1 ), S( 0, 0 )], [S( 0, 1 ), S( 0, 0 )], "defined ASCII" ),
	new( "ere-longest-alternative", true, "(a|aa)", "xaa", [S( 1, 3 ), S( 1, 3 )], [S( 1, 3 ), S( 1, 3 )], "defined ASCII" ),
	new( "ere-subexpression-tie", true, "(a*)(a*)", "aa", [S( 0, 2 ), S( 0, 2 ), S( 2, 2 )], [S( 0, 2 ), S( 0, 2 ), S( 2, 2 )], "defined ASCII" ),
	new( "ere-c-locale-digit-class", true, "([[:digit:]]+)", "x42", [S( 1, 3 ), S( 1, 3 )], [S( 1, 3 ), S( 1, 3 )], "defined ASCII" ),
	new( "ere-longest-before-suffix", true, "(a|aa)b", "xaab", [S( 1, 4 ), S( 1, 3 )], [S( 1, 4 ), S( 1, 3 )], "defined ASCII" ),
	new( "bre-middle-caret-literal", false, "a^b", "a^b", [S( 0, 3 )], [S( 0, 3 )], "Issue 8 §9.3.8; BRE middle caret is ordinary" ),
	new( "bre-middle-dollar-literal", false, "a$b", "a$b", [S( 0, 3 )], [S( 0, 3 )], "Issue 8 §9.3.8; BRE middle dollar is ordinary" ),
	new( "ere-middle-caret-anchor", true, "a^b", "a^b", [], [S( 0, 3 )], "Issue 8 §9.4.9; inherited Extended literal divergence" ),
	new( "ere-middle-dollar-anchor", true, "a$b", "a$b", [], [S( 0, 3 )], "Issue 8 §9.4.9; inherited Extended literal divergence" ),
	new( "ere-escaped-caret-literal", true, @"a\^b", "a^b", [S( 0, 3 )], [S( 0, 3 )], "Issue 8 §9.4.3; escaped anchor is literal" ),
	new( "ere-escaped-dollar-literal", true, @"a\$b", "a$b", [S( 0, 3 )], [S( 0, 3 )], "Issue 8 §9.4.3; escaped anchor is literal" ),
	new( "ere-anchor-first-branch", true, "(^|foo)bar", "bar", [S( 0, 3 ), S( 0, 0 )], [S( 0, 3 ), S( 0, 0 )], "glibc and included Gnulib agree on empty capture" ),
	new( "ere-anchor-second-branch", true, "(foo|^)bar", "bar", [S( 0, 3 ), S( 0, 0 )], [S( 0, 3 ), S( 0, 0 )], "glibc and included Gnulib agree on empty capture" )
};

var failures = 0;
foreach ( var item in cases ) {
	if ( item.Pattern.Any( c => c > 127 ) || item.Input.Any( c => c > 127 ) ) {
		throw new InvalidOperationException( $"{item.Id}: fixture must remain ASCII" );
	}
	var native = NativeMatch( item );
	var managed = ManagedMatch( item );
	var nativePinned = native.SequenceEqual( item.ExpectedNative );
	var managedPinned = managed.SequenceEqual( item.ExpectedManaged );
	Console.WriteLine(
		$"{item.Id}: glibc {Show( native )}; managed {Show( managed )}; {item.Classification}; " +
		( nativePinned && managedPinned ? "pinned" : "DRIFT" )
	);
	if ( !nativePinned || !managedPinned ) {
		Console.Error.WriteLine(
			$"  expected glibc {Show( item.ExpectedNative )}; managed {Show( item.ExpectedManaged )}"
		);
		failures++;
	}
}
Console.WriteLine( $"{cases.Length - failures}/{cases.Length} differential cases pinned" );
return failures == 0 ? 0 : 1;

static SpanRange S( int start, int end ) => new( start, end );
static string Show( IReadOnlyList<SpanRange> spans ) => spans.Count == 0
	? "no match"
	: String.Join( "; ", spans.Select( span => $"({span.Start},{span.End})" ) );

static SpanRange[] ManagedMatch( FixtureCase item ) {
	IRegularExpressionProvider provider = item.Extended
		? new GnuExtendedRegularExpressionProvider( PosixCLocaleRegularExpressionCharacterClassProvider.Instance )
		: new GnuBasicRegularExpressionProvider( PosixCLocaleRegularExpressionCharacterClassProvider.Instance );
	var compiled = provider.Compile( item.Pattern );
	if ( !compiled.IsSuccess ) {
		throw new InvalidOperationException( $"{item.Id}: managed compile {compiled.Diagnostic?.Code}" );
	}
	var result = compiled.Expression!.Match( item.Input );
	if ( !result.IsSuccess ) {
		throw new InvalidOperationException( $"{item.Id}: managed match {result.Diagnostic?.Code}" );
	}
	if ( !result.IsMatch ) {
		return [];
	}
	var match = result.Match!;
	return [new SpanRange( match.Index, match.Index + match.Length ),
		..match.Captures.Select( capture => capture.Success
			? new SpanRange( capture.Index, capture.Index + capture.Length )
			: new SpanRange( -1, -1 ) )];
}

static SpanRange[] NativeMatch( FixtureCase item ) {
	const int RegexMemoryBytes = 4096;
	const int MatchCount = 10;
	var regex = Marshal.AllocHGlobal( RegexMemoryBytes );
	var matches = Marshal.AllocHGlobal( MatchCount * 8 );
	var compiled = false;
	try {
		Marshal.Copy( new byte[ RegexMemoryBytes ], 0, regex, RegexMemoryBytes );
		var code = Native.regcomp( regex, item.Pattern, item.Extended ? 1 : 0 );
		if ( code != 0 ) {
			throw new InvalidOperationException( $"{item.Id}: glibc regcomp returned {code}" );
		}
		compiled = true;
		code = Native.regexec( regex, item.Input, (nuint)MatchCount, matches, 0 );
		if ( code == 1 ) { // POSIX REG_NOMATCH on glibc.
			return [];
		}
		if ( code != 0 ) {
			throw new InvalidOperationException( $"{item.Id}: glibc regexec returned {code}" );
		}
		var spans = new SpanRange[ item.ExpectedNative.Length ];
		for ( var index = 0; index < spans.Length; index++ ) {
			spans[ index ] = new SpanRange(
				Marshal.ReadInt32( matches, index * 8 ),
				Marshal.ReadInt32( matches, index * 8 + 4 )
			);
		}
		return spans;
	} finally {
		if ( compiled ) {
			Native.regfree( regex );
		}
		Marshal.FreeHGlobal( matches );
		Marshal.FreeHGlobal( regex );
	}
}

internal readonly record struct SpanRange( int Start, int End );
internal sealed record FixtureCase(
	string Id, bool Extended, string Pattern, string Input,
	SpanRange[] ExpectedNative, SpanRange[] ExpectedManaged, string Classification
);

internal static class Native {
	[DllImport( "libc.so.6", CallingConvention = CallingConvention.Cdecl )]
	internal static extern IntPtr gnu_get_libc_version();

	[DllImport( "libc.so.6", CallingConvention = CallingConvention.Cdecl )]
	internal static extern IntPtr setlocale( int category, [MarshalAs( UnmanagedType.LPUTF8Str )] string locale );

	[DllImport( "libc.so.6", CallingConvention = CallingConvention.Cdecl )]
	internal static extern int regcomp( IntPtr regex, [MarshalAs( UnmanagedType.LPUTF8Str )] string pattern, int flags );

	[DllImport( "libc.so.6", CallingConvention = CallingConvention.Cdecl )]
	internal static extern int regexec( IntPtr regex, [MarshalAs( UnmanagedType.LPUTF8Str )] string input,
		nuint matchCount, IntPtr matches, int flags );

	[DllImport( "libc.so.6", CallingConvention = CallingConvention.Cdecl )]
	internal static extern void regfree( IntPtr regex );
}
