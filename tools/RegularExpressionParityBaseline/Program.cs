using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

// These are current-engine observations, not claims of POSIX or Gnulib conformance.
// Keep case order and IDs stable: the output is the copy-time parity contract.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var cases = new FixtureCase[] {
	new( "bre-leftmost", "basic", @"a\|aa", "zaa aa" ),
	new( "bre-captures", "basic", @"\(ac*\)\(c*d[ac]*\)\1", "acdacaaa", AtStart: true ),
	new( "bre-capture-absent", "basic", @"\(a\)*", "", AtStart: true ),
	new( "bre-capture-empty", "basic", @"\(\)*", "", AtStart: true ),
	new( "bre-literal-plus", "basic", "ab+", "ab+", AtStart: true ),
	new( "bre-gnu-alternation", "basic", @"a\|aa", "aa", AtStart: true ),
	new( "bre-invalid-duplicate", "basic", "a**" ),
	new( "expr-invalid-duplicate", "basic", "a**", "aaaa", Profile: "expr", AtStart: true ),
	new( "bre-reversed-range", "basic", "[z-a]" ),
	new( "expr-reversed-range", "basic", "[z-a]", "a", Profile: "expr" ),
	new( "bre-tac-emacs-request", "basic", "ab+", "abbb", Profile: "emacs-request", AtStart: true ),
	new( "diff-case-fold", "basic", "error", "Error", Profile: "case-fold" ),
	new( "expr-anchored-capture", "basic", @"\(ab*\)", "abbbx", Profile: "expr", AtStart: true ),
	new( "expr-refuses-later-match", "basic", "a", "ba", Profile: "expr", AtStart: true ),
	new( "bre-pattern-newline", "basic", "a\nb", "a\nb" ),
	new( "bre-mid-anchor", "basic", "a^b", "a^b" ),
	new( "bre-invalid-backref", "basic", @"\1" ),
	new( "bre-unsupported-collation", "basic", "[[.ch.]]" ),
	new( "ere-leftmost-captures", "extended", "(a|aa)([[:digit:]]{2})", "zaa42" ),
	new( "ere-gnu-backreference", "extended", @"(ab)\1", "abab", AtStart: true ),
	new( "ere-invalid-duplicate", "extended", "a**" ),
	new( "gnu-extended-invalid-duplicate", "extended", "a**", "aaaa", Profile: "gnu-extended", AtStart: true ),
	new( "ere-pattern-newline-no-match", "extended", "a\nb", "ab" ),
	new( "ere-dot-excludes-null", "extended", ".", "\0" ),
	new( "ere-dot-allows-null", "extended", ".", "\0", Profile: "dot-null" ),
	new( "ere-line-anchor", "extended", "^b$", "a\nb\nc", Profile: "line-sensitive" ),
	new( "ere-null-line-anchor", "extended", "^b$", "a\0b\0c", Profile: "null-lines" ),
	new( "ere-c-locale-no-match", "extended", "[[:alpha:]]+", "é" ),
	new( "ere-unicode-class", "extended-unicode", "[[:alpha:]]+", "é" ),
	new( "ere-utf16-scalar", "extended", "(.)", "x😀y", Start: 1 ),
	new( "ere-invalid-group", "extended", "(abc" ),
	new( "ere-nesting-limit", "extended", "((a))", Profile: "nest-limit-1" ),
	new( "procps-string-offset", "extended", "(cat|dog)", "xxdog" ),
	new( "ed-basic-capture", "basic", @"\(ab\)", "xaby" ),
	new( "emacs-plus", "emacs", "ab+", "abbb", AtStart: true ),
	new( "ptx-emacs-bounded", "emacs", "ab+", "abb", Profile: "gnu-emacs", AtStart: true ),
	new( "utf8-source-offset", "extended", "(.)", "x😀y", Surface: "utf8", Start: 1 ),
	new( "utf8-split-scalar", "extended", ".", "x😀y", Surface: "utf8", Start: 2 ),
	new( "malformed-utf8-preserve", "extended", ".", HexInput: "FF", Surface: "utf8" ),
	new( "opaque-byte", "extended", ".", HexInput: "FF", Surface: "bytes" ),
	new( "prepared-byte-capture", "extended", "(TARGET)", "xxTARGETyy", Surface: "prepared-bytes" ),
	new( "grep-byte-offset", "extended", "a+", "xxaaa", Surface: "bytes", Profile: "gnu-extended", Start: 2 ),
	new( "csplit-byte-line", "basic", "hit", "xxhit", Surface: "utf8", Profile: "expr" ),
	new( "limited-search", "extended", "(a|aa)*b", "aaaaaaaaaaaaaaaa", Profile: "state-limit-10" )
};

var jsonOptions = new JsonSerializerOptions {
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
var lines = cases.Select( item => JsonSerializer.Serialize( Evaluate( item ), jsonOptions ) ).ToArray();
var baseline = String.Join( "\n", lines ) + "\n";
if ( args.Length == 0 ) {
	Console.OutputEncoding = new UTF8Encoding( false );
	Console.Write( baseline );
} else if ( args.Length == 2 && args[ 0 ] == "--verify" ) {
	var expected = File.ReadAllText( args[ 1 ], Encoding.UTF8 );
	if ( expected != baseline ) {
		var previous = expected.Split( '\n' );
		var actual = baseline.Split( '\n' );
		var first = Enumerable.Range( 0, Math.Max( previous.Length, actual.Length ) )
			.First( index => index >= previous.Length || index >= actual.Length || previous[ index ] != actual[ index ] );
		Console.Error.WriteLine( $"Parity mismatch at line {first + 1}: {cases[ Math.Min( first, cases.Length - 1 ) ].Id}" );
		Console.Error.WriteLine( $"Expected: {( first < previous.Length ? previous[ first ] : "<end>" )}" );
		Console.Error.WriteLine( $"Actual:   {( first < actual.Length ? actual[ first ] : "<end>" )}" );
		Environment.ExitCode = 1;
	}
} else {
	Console.Error.WriteLine( "Usage: RegularExpressionParityBaseline [--verify <baseline.jsonl>]" );
	Environment.ExitCode = 2;
}

static object Evaluate( FixtureCase item ) {
	IRegularExpressionCharacterClassProvider classes = item.Provider == "extended-unicode"
		? UnicodeRegularExpressionCharacterClassProvider.InvariantCulture
		: PosixCLocaleRegularExpressionCharacterClassProvider.Instance;
	IRegularExpressionProvider provider = item.Provider switch {
		"basic" => new GnuBasicRegularExpressionProvider( classes ),
		"extended" or "extended-unicode" => new GnuExtendedRegularExpressionProvider( classes ),
		"emacs" => new GnuEmacsRegularExpressionProvider( classes ),
		_ => throw new ArgumentException( item.Provider )
	};
	RegularExpressionOptions? options = item.Profile switch {
		"default" => null,
		"expr" => RegularExpressionOptions.GnuExprCompatibility,
		"gnu-extended" => RegularExpressionOptions.GnuExtendedCompatibility,
		"gnu-emacs" => RegularExpressionOptions.GnuEmacsCompatibility,
		"emacs-request" => RegularExpressionOptions.GnuEmacsCompatibility,
		"case-fold" => new() { IgnoreCase = true },
		"dot-null" => new() { DotMatchesNull = true },
		"nest-limit-1" => new() { MaximumNestingDepth = 1 },
		"line-sensitive" => new() { NewLineSensitive = true },
		"null-lines" => new() { NewLineSensitive = true, LineSeparator = new Rune( '\0' ), DotMatchesNull = true },
		"state-limit-10" => new() { MaximumMatchStates = 10 },
		_ => throw new ArgumentException( item.Profile )
	};
	var compiled = provider.Compile( item.Pattern, options );
	object result;
	if ( !compiled.IsSuccess ) {
		result = new { status = "compile-error", diagnostic = compiled.Diagnostic!.Code.ToString(), index = compiled.Diagnostic.PatternIndex };
	} else if ( item.Input is null && item.HexInput is null ) {
		throw new InvalidOperationException( $"Missing input: {item.Id}" );
	} else if ( item.Surface == "string" ) {
		var found = compiled.Expression!.Match(
			item.Input!,
			new RegularExpressionMatchOptions { StartIndex = item.Start, RequireMatchAtStart = item.AtStart }
		);
		result = found.IsSuccess
			? new { status = found.IsMatch ? "match" : "no-match", match = found.Match is null ? null : new {
				index = found.Match.Index, length = found.Match.Length, value = found.Match.Value,
				captures = found.Match.Captures.Select( capture => new {
					success = capture.Success, index = capture.Index, length = capture.Length, value = capture.Value
				} ).ToArray()
			} } as object
			: new { status = "match-error", diagnostic = found.Diagnostic!.Code.ToString() };
	} else {
		var bytes = item.HexInput is null ? Encoding.UTF8.GetBytes( item.Input! ) : Convert.FromHexString( item.HexInput );
		var inputOptions = new RegularExpressionInputOptions {
			DecodingMode = item.Surface == "utf8" ? TextDecodingMode.Utf8 : TextDecodingMode.Bytes,
			InvalidEncodingPolicy = InvalidEncodingPolicy.PreserveBytes
		};
		var matchOptions = new RegularExpressionByteMatchOptions { StartByteOffset = item.Start, RequireMatchAtStart = item.AtStart };
		var found = item.Surface == "prepared-bytes"
			? compiled.Expression!.Match( RegularExpressionPreparedByteInput.Prepare( bytes, inputOptions ), matchOptions )
			: compiled.Expression!.Match( bytes, inputOptions, matchOptions );
		result = found.IsSuccess
			? new { status = found.IsMatch ? "match" : "no-match", match = found.Match is null ? null : new {
				index = found.Match.ByteIndex, length = found.Match.ByteLength, hex = Convert.ToHexString( found.Match.Value.Span ),
				captures = found.Match.Captures.Select( capture => new {
					success = capture.Success, index = capture.ByteIndex, length = capture.ByteLength, hex = capture.Success ? Convert.ToHexString( capture.Value.Span ) : null
				} ).ToArray()
			} } as object
			: new { status = "match-error", diagnostic = found.Diagnostic!.Code.ToString() };
	}
	return new {
		id = item.Id,
		provider = item.Provider,
		profile = item.Profile,
		surface = item.Surface,
		pattern = item.Pattern,
		input = item.Input,
		inputHex = item.HexInput,
		start = item.Start,
		atStart = item.AtStart,
		result
	};
}

internal sealed record FixtureCase(
	string Id,
	string Provider,
	string Pattern,
	string? Input = null,
	string? HexInput = null,
	string Surface = "string",
	string Profile = "default",
	int Start = 0,
	bool AtStart = false
);
