namespace Icod.CommandFramework.Text;

/// <summary>Identifies how tab stops continue after the explicit stop list.</summary>
public enum TabStopContinuationKind {
	/// <summary>No tab stops exist after the final explicit stop.</summary>
	None,
	/// <summary>Stops continue at global multiples of the configured interval.</summary>
	Absolute,
	/// <summary>Stops continue at interval offsets from the final explicit stop.</summary>
	Relative
}
