# Observation fidelity

`Icod.CommandFramework.Host` retains one consumer-side semantic contract:
`ObservationFidelity`.

`ObservationFidelity` describes how faithfully an observation represents the
authoritative source semantics:

- `Exact`
- `Equivalent`
- `Approximated`
- `Synthesized`
- `Unavailable`

The enum is intentionally retained in `Icod.CommandFramework` because it
describes how a consumer should characterize observed data rather than how the
host obtains that data.

Beginning with `Icod.CommandFramework` 2.0.0, factual host identifiers,
processor-resource observations, topology, affinity, quota discovery, and the
associated provider contracts are no longer implemented here. Use the
standalone `Icod.Host` package for those facilities.

ProcPs-specific observation models remain owned by `Icod.ProcPs.Shared`; they
may use their own domain fidelity vocabulary where that better represents
ProcPs semantics.
