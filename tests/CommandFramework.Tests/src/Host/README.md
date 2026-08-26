# Semantic-fidelity tests

Factual host identity and processor-resource tests have moved to the standalone
`Icod.Host` repository and package.

This directory retains only `ObservationFidelityTests` because
`ObservationFidelity` remains consumer semantic policy owned by
`Icod.CommandFramework`; it was intentionally not migrated to `Icod.Host`.
