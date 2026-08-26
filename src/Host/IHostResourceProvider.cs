namespace Icod.CommandFramework.Host;

/// <summary>Supplies normalized host-identifier observations.</summary>
[Obsolete("Use the Icod.Host package instead.")]
public interface IHostIdentifierProvider {
	/// <summary>Gets the current host identifier.</summary>
	/// <param name="cancellationToken">A token used to cancel the observation.</param>
	/// <returns>The host-identifier observation.</returns>
	ValueTask<HostResourceValue<HostIdentifier>> GetHostIdentifierAsync(
		CancellationToken cancellationToken = default
	);
}

/// <summary>Supplies processor-resource observations.</summary>
[Obsolete("Use the Icod.Host package instead.")]
public interface IProcessorResourceProvider {
	/// <summary>Gets processor-resource facts for the host and current process.</summary>
	/// <param name="cancellationToken">A token used to cancel the observation.</param>
	/// <returns>The processor-resource snapshot.</returns>
	ValueTask<ProcessorResourceSnapshot> GetProcessorResourcesAsync(
		CancellationToken cancellationToken = default
	);
}

/// <summary>
/// Supplies the combined host and processor-resource foundation used by
/// Coreutils and later cross-suite consumers.
/// </summary>
[Obsolete("Use the Icod.Host package instead.")]
public interface IHostResourceProvider : IHostIdentifierProvider, IProcessorResourceProvider {
	/// <summary>Gets a combined host-resource snapshot.</summary>
	/// <param name="cancellationToken">A token used to cancel the observation.</param>
	/// <returns>The combined snapshot.</returns>
	ValueTask<HostResourceSnapshot> ObserveAsync(
		CancellationToken cancellationToken = default
	);
}
