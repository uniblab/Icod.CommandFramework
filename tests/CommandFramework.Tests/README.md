# Icod.CommandFramework tests

`Icod.CommandFramework.Tests` verifies only APIs and mechanisms that remain
owned by `Icod.CommandFramework`.

Only tests for production subsystems retained by the 2.0.0 package remain in
this repository.

## Layout

Tests under `src` mirror the production subsystem that owns the behavior being
tested. New tests should be placed under the corresponding subsystem directory
rather than at the test-project root.

Release builds treat warnings as errors except for the repository's explicit
documentation-warning exemption.
