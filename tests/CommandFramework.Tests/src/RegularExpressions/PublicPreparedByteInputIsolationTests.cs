namespace Icod.CommandFramework.Tests;

using System.Runtime.InteropServices;

using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

using Xunit;

public sealed class PublicPreparedByteInputIsolationTests {
	private static readonly RegularExpressionInputOptions ByteInputOptions = new() {
		DecodingMode = TextDecodingMode.Bytes
	};

	[Fact]
	public void ExternalFallbackCannotMutatePreparedSnapshot() {
		var prepared = RegularExpressionPreparedByteInput.Prepare(
			"xxTARGETyy"u8.ToArray(),
			ByteInputOptions
		);
		var mutating = new MutatingCompiledExpression();

		var first = mutating.Match( prepared );
		var second = Compile( "TARGET" ).Match( prepared );

		Assert.True( first.IsSuccess );
		Assert.True( second.IsMatch );
		Assert.Equal( 2, second.Match!.ByteIndex );
		Assert.Equal( "TARGET"u8.ToArray(), second.Match.Value.ToArray() );
	}

	private static ICompiledRegularExpression Compile( string pattern ) {
		var provider = new GnuBasicRegularExpressionProvider(
			PosixCLocaleRegularExpressionCharacterClassProvider.Instance
		);
		var result = provider.Compile( pattern );
		Assert.True( result.IsSuccess, result.Diagnostic?.Message );
		return Assert.IsAssignableFrom<ICompiledRegularExpression>( result.Expression );
	}

	private sealed class MutatingCompiledExpression : ICompiledRegularExpression {
		public string Pattern => string.Empty;

		public int CaptureCount => 0;

		public RegularExpressionMatchResult Match(
			string input,
			RegularExpressionMatchOptions? options = null,
			CancellationToken cancellationToken = default
		) => RegularExpressionMatchResult.Succeeded( null );

		public ValueTask<RegularExpressionMatchResult> MatchAsync(
			string input,
			RegularExpressionMatchOptions? options = null,
			CancellationToken cancellationToken = default
		) => ValueTask.FromResult( Match( input, options, cancellationToken ) );

		public RegularExpressionByteMatchResult Match(
			ReadOnlyMemory<byte> input,
			RegularExpressionInputOptions? inputOptions = null,
			RegularExpressionByteMatchOptions? matchOptions = null,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if (
				MemoryMarshal.TryGetArray(
					input,
					out ArraySegment<byte> exposed
				)
				&& exposed.Array is not null
				&& 2 < exposed.Count
			) {
				exposed.Array[ exposed.Offset + 2 ] = (byte)'X';
			}
			return RegularExpressionByteMatchResult.Succeeded( null );
		}

		public ValueTask<RegularExpressionByteMatchResult> MatchAsync(
			ReadOnlyMemory<byte> input,
			RegularExpressionInputOptions? inputOptions = null,
			RegularExpressionByteMatchOptions? matchOptions = null,
			CancellationToken cancellationToken = default
		) => ValueTask.FromResult(
			Match( input, inputOptions, matchOptions, cancellationToken )
		);
	}
}
