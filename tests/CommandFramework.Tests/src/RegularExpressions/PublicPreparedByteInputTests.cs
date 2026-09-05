namespace Icod.CommandFramework.Tests;

using System.Text;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

using Xunit;

public sealed class PublicPreparedByteInputTests {
	private static readonly RegularExpressionInputOptions ByteInputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};

	[Fact]
	public void PublicPreparedByteInputReusesImmutableSnapshot() {
		var expression = Compile( "TARGET" );
		var source = "xxTARGETyy"u8.ToArray();
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			source,
			ByteInputOptions
		);
		source[ 2 ] = (byte)'X';

		var first = expression.Match( prepared );
		var second = expression.Match(
			prepared,
			new RegularExpressionByteMatchOptions { StartByteOffset = 3 }
		);

		Assert.True( first.IsMatch );
		Assert.Equal( 2, first.Match!.ByteIndex );
		Assert.Equal( "TARGET"u8.ToArray(), first.Match.Value.ToArray() );
		Assert.True( second.IsSuccess );
		Assert.False( second.IsMatch );
		Assert.Equal( source.Length, prepared.Length );
		Assert.Equal( ByteInputOptions, prepared.InputOptions );
	}

	[Fact]
	public async Task PublicPreparedByteInputSupportsConcurrentAsyncMatches() {
		var expression = Compile( "TARGET" );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			Encoding.ASCII.GetBytes( "TARGET----TARGET----TARGET" ),
			ByteInputOptions
		);
		var offsets = new[] { 0, 1, 10, 11, 20 };

		var tasks = Enumerable.Range( 0, 128 )
			.Select(
				async index => await expression.MatchAsync(
					prepared,
					new RegularExpressionByteMatchOptions {
						StartByteOffset = offsets[ index % offsets.Length ]
					}
				)
			)
			.ToArray();
		var results = await Task.WhenAll( tasks );

		for ( var index = 0; results.Length > index; index++ ) {
			var result = results[ index ];
			Assert.True( result.IsSuccess );
			Assert.True( result.IsMatch );
			var expectedIndex = offsets[ index % offsets.Length ] switch {
				0 => 0,
				1 or 10 => 10,
				11 or 20 => 20,
				_ => throw new InvalidOperationException()
			};
			Assert.Equal( expectedIndex, result.Match!.ByteIndex );
		}
	}

	[Fact]
	public void PublicPreparedByteInputPreservesUtf8BoundaryValidation() {
		var expression = Compile( "TARGET" );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			Encoding.UTF8.GetBytes( "éTARGET" ),
			new RegularExpressionInputOptions {
				DecodingMode = TextDecodingMode.Utf8
			}
		);

		var match = expression.Match(
			prepared,
			new RegularExpressionByteMatchOptions { StartByteOffset = 2 }
		);
		var split = expression.Match(
			prepared,
			new RegularExpressionByteMatchOptions { StartByteOffset = 1 }
		);

		Assert.True( match.IsMatch );
		Assert.Equal( 2, match.Match!.ByteIndex );
		Assert.False( split.IsSuccess );
		Assert.Equal(
			RegularExpressionDiagnosticCode.InvalidStartByteOffset,
			split.Diagnostic!.Code
		);
	}

	[Fact]
	public void PublicPreparedByteInputFallsBackForExternalCompiledExpressions() {
		var fallback = new DelegatingCompiledExpression( Compile( "TARGET" ) );
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			"xxTARGETyy"u8.ToArray(),
			ByteInputOptions
		);

		var result = fallback.Match( prepared );

		Assert.True( result.IsMatch );
		Assert.Equal( 2, result.Match!.ByteIndex );
		Assert.Equal( 1, fallback.ByteMatchCalls );
	}

	private static ICompiledRegularExpression Compile( string pattern ) {
		var provider = new GnuBasicRegularExpressionProvider(
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var result = provider.Compile( pattern );
		Assert.True( result.IsSuccess, result.Diagnostic?.Message );
		return Assert.IsAssignableFrom<ICompiledRegularExpression>( result.Expression );
	}

	private sealed class DelegatingCompiledExpression : ICompiledRegularExpression {
		private readonly ICompiledRegularExpression inner;

		internal DelegatingCompiledExpression( ICompiledRegularExpression inner ) {
			this.inner = inner;
		}

		internal int ByteMatchCalls { get; private set; }

		public string Pattern => this.inner.Pattern;

		public int CaptureCount => this.inner.CaptureCount;

		public RegularExpressionMatchResult Match(
			string input,
			RegularExpressionMatchOptions? options = null,
			CancellationToken cancellationToken = default
		) => this.inner.Match( input, options, cancellationToken );

		public ValueTask<RegularExpressionMatchResult> MatchAsync(
			string input,
			RegularExpressionMatchOptions? options = null,
			CancellationToken cancellationToken = default
		) => this.inner.MatchAsync( input, options, cancellationToken );

		public RegularExpressionByteMatchResult Match(
			ReadOnlyMemory<byte> input,
			RegularExpressionInputOptions? inputOptions = null,
			RegularExpressionByteMatchOptions? matchOptions = null,
			CancellationToken cancellationToken = default
		) {
			ByteMatchCalls++;
			return this.inner.Match(
				input,
				inputOptions,
				matchOptions,
				cancellationToken
			);
		}

		public ValueTask<RegularExpressionByteMatchResult> MatchAsync(
			ReadOnlyMemory<byte> input,
			RegularExpressionInputOptions? inputOptions = null,
			RegularExpressionByteMatchOptions? matchOptions = null,
			CancellationToken cancellationToken = default
		) {
			ByteMatchCalls++;
			return this.inner.MatchAsync(
				input,
				inputOptions,
				matchOptions,
				cancellationToken
			);
		}
	}
}
