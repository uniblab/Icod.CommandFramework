# Icod.CommandFramework tests

`Icod.CommandFramework.Tests` verifies only APIs and mechanisms that remain
owned by `Icod.CommandFramework`.

Beginning with 2.0.0, the former timing, factual-host, process, and terminal
compatibility APIs are no longer present in this package. Their tests belong to
`Icod.Timing`, `Icod.Host`, `Icod.Processes`, and `Icod.Terminal`; terminal
database and curses tests belong to `Icod.TermInfo` and `Icod.DCurses`.

`ObservationFidelity` remains consumer semantic policy owned by
`Icod.CommandFramework`, so its test remains under `src/Host`.

## Layout

Tests under `src` mirror the production subsystem that owns the behavior being
tested. New tests should be placed under the corresponding subsystem directory
rather than at the test-project root.

Release builds treat warnings as errors except for the repository's explicit
documentation-warning exemption.
