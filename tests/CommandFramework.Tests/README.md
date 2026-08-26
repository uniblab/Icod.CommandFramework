# Icod.CommandFramework tests

`Icod.CommandFramework.Tests` verifies only APIs and mechanisms that remain owned
by `Icod.CommandFramework`.

Tests for APIs extracted into `Icod.Timing`, `Icod.Host`, `Icod.Processes`,
`Icod.Terminal`, `Icod.TermInfo`, and `Icod.DCurses` belong to those packages
and are not duplicated here. The obsolete compatibility surfaces retained by
`Icod.CommandFramework` are therefore build compatibility code, not a second
source of behavioral test ownership.

`ObservationFidelity` is the intentional exception: it remains consumer
semantic policy owned by `Icod.CommandFramework`, so its test remains under
`src/Host`.

## Layout

Tests under `src` mirror the production subsystem that owns the behavior being
tested. New tests should be placed under the corresponding subsystem directory
rather than at the test-project root.

Release builds do not exempt `CS0618`; a test that starts depending on an
obsolete compatibility API should fail the Release build and be moved to the
owning standalone package instead.
