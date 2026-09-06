[CmdletBinding()]
param(
    [string]$BaselineCommit = '3c7b8189f664be67b74fb71809580d3133dc135f',
    [string]$BaselineLabel = 'CommandFramework-2.2.0',
    [string]$OutputDirectory = 'artifacts/performance/byte-input-construction',
    [ValidateRange(1, 8)]
    [int]$Passes = 2,
    [ValidateRange(0, 600)]
    [int]$CooldownSeconds = 30,
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (git rev-parse --show-toplevel).Trim()
if (0 -ne $LASTEXITCODE -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw 'Unable to resolve the repository root.'
}

function Write-IcodProgressLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host ('[{0}] {1}' -f [DateTimeOffset]::Now.ToString('HH:mm:ss'), $Message)
}

Push-Location $repoRoot
try {
    if (-not $AllowDirty) {
        [object[]]$dirty = @(git status --porcelain)
        if (0 -ne $LASTEXITCODE) {
            throw 'Unable to inspect repository status.'
        }
        if (0 -lt $dirty.Length) {
            throw 'The authoritative byte-input construction comparison requires a clean worktree. Commit/stash changes or use -AllowDirty for an explicitly non-authoritative run.'
        }
    }

    $candidateCommit = (git rev-parse HEAD).Trim()
    if (0 -ne $LASTEXITCODE -or [string]::IsNullOrWhiteSpace($candidateCommit)) {
        throw 'Unable to resolve the candidate commit.'
    }

    git cat-file -e "$BaselineCommit^{commit}" 2>$null
    if (0 -ne $LASTEXITCODE) {
        git fetch origin $BaselineCommit --depth=1
        if (0 -ne $LASTEXITCODE) {
            throw "Unable to fetch baseline commit $BaselineCommit."
        }
    }

    $project = 'benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj'
    $benchmarkFile = 'benchmarks/PreparedRegularExpressions.Benchmarks/PreparedByteInputConstructionBenchmarks.cs'
    $validator = Join-Path $repoRoot 'benchmarks/Assert-BenchmarkDotNetRun.ps1'
    $outputRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
    if (Test-Path -LiteralPath $outputRoot) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

    $repoParent = Split-Path -Parent $repoRoot
    $temporaryBase = [System.IO.Path]::GetTempPath()
    $temporaryName = 'icf-r2-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $temporaryRoot = Join-Path $temporaryBase $temporaryName
    $baselineRoot = Join-Path $temporaryRoot 'b'
    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null

    Write-IcodProgressLine "Preparing $BaselineLabel worktree at $baselineRoot."
    git worktree add --detach $baselineRoot $BaselineCommit
    if (0 -ne $LASTEXITCODE) {
        throw "Unable to create the $BaselineLabel worktree."
    }

    try {
        $baselineBenchmarkPath = Join-Path $baselineRoot $benchmarkFile
        $baselineBenchmarkDirectory = Split-Path -Parent $baselineBenchmarkPath
        New-Item -ItemType Directory -Path $baselineBenchmarkDirectory -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repoRoot $benchmarkFile) -Destination $baselineBenchmarkPath -Force

        function Initialize-IcodVariant {
            param(
                [Parameter(Mandatory = $true)]
                [string]$Root,
                [Parameter(Mandatory = $true)]
                [string]$Label
            )

            Write-IcodProgressLine "Restoring/building $Label benchmark harness."
            Push-Location $Root
            try {
                & dotnet restore $project | Out-Host
                if (0 -ne $LASTEXITCODE) {
                    throw "$Label benchmark restore failed."
                }
                & dotnet build $project -c Release --no-restore -p:ContinuousIntegrationBuild=true | Out-Host
                if (0 -ne $LASTEXITCODE) {
                    throw "$Label benchmark build failed."
                }
            } finally {
                Pop-Location
            }
        }

        function Invoke-IcodVariant {
            param(
                [Parameter(Mandatory = $true)]
                [string]$Root,
                [Parameter(Mandatory = $true)]
                [string]$Label,
                [Parameter(Mandatory = $true)]
                [string]$Commit,
                [Parameter(Mandatory = $true)]
                [int]$Pass,
                [Parameter(Mandatory = $true)]
                [int]$RunNumber,
                [Parameter(Mandatory = $true)]
                [int]$RunCount
            )

            $passLabel = "$Label-pass-$Pass"
            $passOutput = Join-Path $outputRoot $passLabel
            New-Item -ItemType Directory -Path $passOutput -Force | Out-Null
            $watch = [System.Diagnostics.Stopwatch]::StartNew()
            Write-IcodProgressLine "Starting benchmark run $RunNumber/${RunCount}: $passLabel ($($Commit.Substring(0, 7)))."

            Push-Location $Root
            try {
                & dotnet run --project $project -c Release --no-build --no-restore -- --inProcess --artifacts $passOutput --filter '*PreparedByteInputConstructionBenchmarks*' | Out-Host
                $exitCode = $LASTEXITCODE
            } finally {
                Pop-Location
                $watch.Stop()
            }
            if (0 -ne $exitCode) {
                throw "$passLabel benchmark run failed."
            }

            & $validator -ArtifactDirectory $passOutput
            if (0 -ne $LASTEXITCODE) {
                throw "$passLabel benchmark artifact validation failed."
            }

            return [PSCustomObject]@{
                Label = $Label
                Pass = $Pass
                Output = $passLabel
                Commit = $Commit
                ElapsedSeconds = [Math]::Round($watch.Elapsed.TotalSeconds, 3)
            }
        }

        Initialize-IcodVariant -Root $baselineRoot -Label $BaselineLabel
        Initialize-IcodVariant -Root $repoRoot -Label 'candidate'

        $totalRuns = 2 * $Passes
        $sequence = New-Object System.Collections.Generic.List[object]
        $runNumber = 0
        for ($pass = 1; $pass -le $Passes; $pass++) {
            if (0 -eq ($pass % 2)) {
                $variants = @(
                    [PSCustomObject]@{ Root = $repoRoot; Label = 'candidate'; Commit = $candidateCommit },
                    [PSCustomObject]@{ Root = $baselineRoot; Label = $BaselineLabel; Commit = $BaselineCommit }
                )
            } else {
                $variants = @(
                    [PSCustomObject]@{ Root = $baselineRoot; Label = $BaselineLabel; Commit = $BaselineCommit },
                    [PSCustomObject]@{ Root = $repoRoot; Label = 'candidate'; Commit = $candidateCommit }
                )
            }

            foreach ($variant in $variants) {
                $runNumber++
                $sequence.Add(
                    (Invoke-IcodVariant -Root $variant.Root -Label $variant.Label -Commit $variant.Commit -Pass $pass -RunNumber $runNumber -RunCount $totalRuns)
                )
                if (0 -lt $CooldownSeconds -and $runNumber -lt $totalRuns) {
                    Write-IcodProgressLine "Cooling down for $CooldownSeconds seconds before the next benchmark run."
                    Start-Sleep -Seconds $CooldownSeconds
                }
            }
        }

        $defaultInventoryPath = Join-Path $repoParent 'Icod.Grep/hardware_inventory.txt'
        $inventoryHash = if (Test-Path -LiteralPath $defaultInventoryPath -PathType Leaf) {
            (Get-FileHash -LiteralPath $defaultInventoryPath -Algorithm SHA256).Hash.ToLowerInvariant()
        } else {
            $null
        }
        $comparison = [PSCustomObject]@{
            SchemaVersion = 1
            BaselineCommit = $BaselineCommit
            CandidateCommit = $candidateCommit
            BenchmarkClass = 'PreparedByteInputConstructionBenchmarks'
            Passes = $Passes
            CooldownSeconds = $CooldownSeconds
            Sequence = $sequence.ToArray()
            HardwareInventorySha256 = $inventoryHash
            CollectedUtc = [DateTimeOffset]::UtcNow.ToString('O')
        } | ConvertTo-Json -Depth 6
        [System.IO.File]::WriteAllText(
            (Join-Path $outputRoot 'comparison.json'),
            $comparison,
            [System.Text.UTF8Encoding]::new($false)
        )

        Write-IcodProgressLine "Byte-input construction comparison complete. Results: $outputRoot"
    } finally {
        Write-IcodProgressLine "Removing temporary $BaselineLabel worktree."
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            & git worktree remove --force $baselineRoot 2>$null
            $worktreeExitCode = $LASTEXITCODE
            if (0 -ne $worktreeExitCode) {
                Write-Warning "Git could not remove the temporary worktree cleanly. Pruning its registration and continuing because benchmark data collection has already completed."
                & git worktree prune 2>$null | Out-Null
            }
        } finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        if (Test-Path -LiteralPath $temporaryRoot) {
            Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
} finally {
    Pop-Location
}
