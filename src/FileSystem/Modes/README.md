# FileSystem.Modes

`Icod.CommandFramework.FileSystem.Modes` contains command-neutral POSIX mode values and current-process creation-mask observation. GNU symbolic-mode grammar and command-specific mode policy remain in the consuming suite.

## Creation mask

`IFileCreationMaskProvider` exposes the current process file-creation mask as `FileCreationMask`. `SystemFileCreationMaskProvider.Instance` is the default host implementation.

- Windows has no POSIX `umask` concept and returns `FileCreationMask.None`.
- Linux first reads `Umask:` from `/proc/self/status`, avoiding any process-state mutation.
- Linux and best-effort FreeBSD fall back to a process-local locked native `umask(0)` / restore sequence when a non-mutating observation is unavailable.
- macOS uses the same locked query-and-restore pattern through `libSystem`.

The provider observes host mechanism only. It does not decide how omitted GNU symbolic-mode subjects, explicit numeric modes, directory set-ID preservation, or command-specific creation defaults interact with the mask.

## Portable values

`PosixFileMode`, `PosixFileModeBits`, and `FileCreationMask` remain the neutral value vocabulary used by filesystem mutation and suite-level mode policy.

## Concurrency note

POSIX `umask` is process-wide. The query-and-restore fallback is serialized within this provider so two framework callers cannot overlap the temporary zero mask. Linux avoids that window entirely when `/proc/self/status` is available.
