namespace Icod.CommandFramework.FileSystem;

using Icod.CommandFramework.Platform;

/// <summary>
/// Supplies injectable, capability-aware durable-flush, sparse-file, and file-clone operations.
/// Implementations never take ownership of caller-supplied streams.
/// </summary>
/// <remarks>
/// The caller must keep every supplied stream open and must not concurrently dispose it or mutate its
/// native position until the returned operation has completed. Implementations preserve the managed
/// stream position where the individual operation documents that behavior.
/// </remarks>
public interface IFileSystemOperations {
	/// <summary>Gets the operating-system API capability report.</summary>
	FileSystemCapabilities Capabilities { get; }

	/// <summary>Flushes a specific file using the requested durability semantics.</summary>
	ValueTask<PlatformOperationResult> FlushFileAsync(
		FileStream file,
		FileFlushMode mode,
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Opens and flushes a pathname using the requested durability semantics.
	/// Implementations should support directories and special files where the host APIs permit it.
	/// </summary>
	ValueTask<PlatformOperationResult> FlushFileAsync(
		string path,
		FileFlushMode mode,
		CancellationToken cancellationToken = default
	) => ValueTask.FromResult(
		PlatformOperationResult.Unsupported(
			"pathname-specific file flushing is not implemented by this provider"
		)
	);

	/// <summary>Flushes the filesystem containing the supplied path.</summary>
	ValueTask<PlatformOperationResult> FlushFileSystemAsync(
		string path,
		CancellationToken cancellationToken = default
	);

	/// <summary>Requests a flush of all mounted filesystems.</summary>
	ValueTask<PlatformOperationResult> FlushAllFileSystemsAsync(
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Attempts to clone the complete source file into the destination by sharing physical storage.
	/// </summary>
	/// <remarks>
	/// This is a host mechanism, not a command policy. Implementations preserve both managed stream
	/// positions. A successful clone may change the destination length and contents. A controlled
	/// unsupported result means callers may choose an ordinary-copy fallback.
	/// </remarks>
	/// <param name="source">The readable source file.</param>
	/// <param name="destination">The writable destination file.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The explicit platform operation result.</returns>
	ValueTask<PlatformOperationResult> CloneFileAsync(
		FileStream source,
		FileStream destination,
		CancellationToken cancellationToken = default
	) => ValueTask.FromResult(
		PlatformOperationResult.Unsupported(
			"copy-on-write file cloning is not implemented by this provider"
		)
	);

	/// <summary>
	/// Extends a file while requesting sparse allocation semantics and preserves the stream position.
	/// </summary>
	ValueTask<PlatformOperationResult<SparseExtensionInfo>> ExtendSparseAsync(
		FileStream file,
		long newLength,
		CancellationToken cancellationToken = default
	);

	/// <summary>Queries allocated logical ranges for an open file without changing its stream position.</summary>
	ValueTask<PlatformOperationResult<FileAllocationMap>> GetAllocatedRangesAsync(
		FileStream file,
		CancellationToken cancellationToken = default
	);

	/// <summary>Queries allocated logical ranges for a pathname.</summary>
	ValueTask<PlatformOperationResult<FileAllocationMap>> GetAllocatedRangesAsync(
		string path,
		CancellationToken cancellationToken = default
	);
}
