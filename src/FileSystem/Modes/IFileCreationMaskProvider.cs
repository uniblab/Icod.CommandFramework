namespace Icod.CommandFramework.FileSystem.Modes;

/// <summary>
/// Supplies the current process file-creation mask used by commands that need POSIX creation semantics.
/// </summary>
public interface IFileCreationMaskProvider {
	/// <summary>
	/// Gets the current ordinary-permission creation mask.
	/// </summary>
	/// <returns>
	/// The current mask, or <see cref="FileCreationMask.None"/> on hosts without a POSIX creation-mask concept.
	/// </returns>
	FileCreationMask GetCurrentMask();
}
