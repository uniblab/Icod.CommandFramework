# Icod.CommandFramework 2.2.0 — R2.5 Grep Consumer Validation

**Tranche:** R2.5 — prerelease package and Icod.Grep consumer validation  
**CommandFramework candidate:** `76585ce6bdc35dd9b68f0bffa8e4f5c951033de8`  
**Validation package:** `Icod.CommandFramework 2.2.0-alpha.112`  
**Grep baseline:** `423c0e9623100492fa01b6e4d14c183761d111d7` (`Icod.Grep 1.5.0`)  
**Grep measured candidate:** `d4da9d847a6642b735c3e0bc6133109ab58c516d`  
**Status:** accepted; R2.5 closed

## Purpose

R2.5 validates that the accepted CommandFramework 2.2.0 regex optimizations survive a real package boundary and materially improve the motivating consumer, Icod.Grep, without changing GNU grep behavior.

The Grep consumer uses the new public immutable `RegularExpressionPreparedByteInput` surface so one byte record is decoded/prepared once and reused across repeated managed BRE/ERE start-offset attempts and match enumeration.

## Package and semantic gate

`Icod.CommandFramework 2.2.0-alpha.112` was produced from PR #9 and successfully consumed by Icod.Grep PR #12.

The dependency-only Grep integration and the prepared-input consumer integration both passed the complete Grep CI matrix. Canonical Grep workflow **33938715547** was green across:

- Windows, Linux, and macOS builds/tests;
- benchmark smokes;
- packed-tool installation/execution smokes; and
- release-archive smokes for supported runtime targets.

No GNU grep semantic change was required for prepared-input reuse.

## Authoritative physical comparison

The uploaded authoritative comparison records:

- pinned Grep baseline commit `423c0e9623100492fa01b6e4d14c183761d111d7`;
- candidate commit `d4da9d847a6642b735c3e0bc6133109ab58c516d`;
- full benchmark filter `*`;
- two non-smoke passes;
- 30-second cooldown;
- Windows `Microsoft Windows 10.0.26200`;
- .NET `10.0.11`;
- concurrent workstation GC; and
- reference hardware SHA-256 `d73c6e3314dc77d24dd2b28a51221d9b77b5cc6b9796ae00fe8c9c0d92821c9b`.

The two-pass allocation results are highly stable.

| Workload | Baseline allocation | Candidate allocation | Reduction | Baseline mean time | Candidate mean time | Mean time reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| BRE ASCII sparse | 604.60 MiB | 41.27 MiB | **93.17%** | 156.55 ms | 21.10 ms | **86.52%** |
| ERE ASCII dense | 618.66 MiB | 334.99 MiB | **45.85%** | 171.95 ms | 142.90 ms | **16.89%** |
| BRE UTF-8 sparse | 462.87 MiB | 28.83 MiB | **93.77%** | 130.45 ms | 15.05 ms | **88.46%** |
| BRE long-line | 3,005.86 MiB | 213.73 MiB | **92.89%** | 848.25 ms | 122.41 ms | **85.57%** |
| BRE large-file | 4,836.02 MiB | 329.39 MiB | **93.19%** | 1,594.95 ms | 232.90 ms | **85.40%** |
| many-small-files | 316.84 MiB | 46.19 MiB | **85.42%** | 372.90 ms | 171.90 ms | **53.90%** |
| recursive-tree | 317.42 MiB | 46.74 MiB | **85.28%** | 421.95 ms | 205.10 ms | **51.39%** |

The dense ERE timing result is noisier than the sparse/literal cases: pass 1 measured a small timing regression while pass 2 measured a large improvement. Allocation was stable at approximately 334.99 MiB in both passes, so the retained claim is the repeatable **45.85% allocation reduction**, with timing treated conservatively.

## Controls

Record-reader allocation was unchanged across all measured sizes:

| Record length | Baseline | Candidate |
| --- | ---: | ---: |
| 80 bytes | 6.38 MiB | 6.38 MiB |
| 4,096 bytes | 25.34 MiB | 25.34 MiB |
| 262,144 bytes | 53.54 MiB | 53.54 MiB |

This confirms that the large command-level reduction is in the managed regex path rather than a changed corpus or record-reader behavior.

Fixed-string and PCRE command controls show a small Grep-local allocation increase:

- fixed-100: approximately 5.40 MiB → 5.72 MiB;
- fixed-1000: approximately 3.52 MiB → 3.66 MiB; and
- PCRE lookbehind: 5.82 MiB → 6.13 MiB.

Inspection attributes this to Grep's new per-record `PatternInput` reference wrapper, which is used to carry both authoritative byte memory and an optional prepared managed-regex input. This overhead is outside CommandFramework and does not indicate a regression in the CommandFramework regex engine or package. It is a small Grep-local optimization opportunity and should be cleaned up independently by making the wrapper allocation-free or bypassing it for non-managed matchers.

Control timing is noisy and does not show a stable fixed/PCRE slowdown across both passes.

## Acceptance decision

**R2.5 is accepted and closed.**

The consumer gate demonstrates that:

1. the exact CommandFramework prerelease package is consumable through the normal NuGet package boundary;
2. Grep's complete cross-platform semantic and packaging gates remain green;
3. the public prepared-input API produces the expected command-level benefit in the motivating consumer;
4. the dominant BRE workloads reduce managed allocation by approximately 93%;
5. the largest BRE workloads reduce mean command time by approximately 85–88%;
6. record-reader controls are allocation-identical; and
7. the only observed non-managed allocation delta is attributable to Grep orchestration rather than CommandFramework.

## Release conclusion

`Icod.CommandFramework 2.2.0` has completed R2.0 through R2.5 and is ready to merge to `main` and publish as the stable `2.2.0` release.

After stable publication, Icod.Grep should replace its temporary `2.2.0-alpha.112` dependency with `2.2.0` and continue T6 performance work. The Grep-local `PatternInput` allocation should be removed as part of that continuing consumer optimization, but it does not block the CommandFramework release.
