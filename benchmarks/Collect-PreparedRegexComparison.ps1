[CmdletBinding()]
param(
    [string]$OutputDirectory = 'artifacts/performance/regex-prepared-input-candidate-1',
    [ValidateRange(1, 8)]
    [int]$Passes = 2,
    [ValidateRange(0, 600)]
    [int]$CooldownSeconds = 30,
    [switch]$AllowDirty,
    [switch]$Smoke
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
            throw 'The authoritative prepared-input comparison requires a clean worktree. Commit/stash changes or use -AllowDirty for an explicitly non-authoritative run.'
        }
    }

    $candidateCommit = (git rev-parse HEAD).Trim()
    if (0 -ne $LASTEXITCODE -or [string]::IsNullOrWhiteSpace($candidateCommit)) {
        throw 'Unable to resolve the candidate commit.'
    }

    $project = 'benchmarks/PreparedRegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.PreparedBenchmarks.csproj'
    $validator = Join-Path $repoRoot 'benchmarks/Assert-BenchmarkDotNetRun.ps1'
    $outputRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
    if (Test-Path -LiteralPath $outputRoot) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

    $effectivePasses = if ($Smoke) { 1 } else { $Passes }
    $effectiveCooldownSeconds = if ($Smoke) { 0 } else { $CooldownSeconds }

    Write-IcodProgressLine 'Restoring immutable prepared-input benchmark project.'
    & dotnet restore $project | Out-Host
    if (0 -ne $LASTEXITCODE) {
        throw 'Prepared-input benchmark restore failed.'
    }

    Write-IcodProgressLine 'Building immutable prepared-input benchmark project in Release.'
    & dotnet build $project -c Release --no-restore -p:ContinuousIntegrationBuild=true | Out-Host
    if (0 -ne $LASTEXITCODE) {
        throw 'Prepared-input benchmark build failed.'
    }

    $sequence = New-Object System.Collections.Generic.List[object]
    for ($pass = 1; $pass -le $effectivePasses; $pass++) {
        $passLabel = 'pass-{0}' -f $pass
        $passOutput = Join-Path $outputRoot $passLabel
        $arguments = @(
            'run',
            '--project', $project,
            '-c', 'Release',
            '--no-build',
            '--no-restore',
            '--',
            '--inProcess',
            '--artifacts', $passOutput,
            '--filter', '*PreparedRegexInputBenchmarks*'
        )
        if ($Smoke) {
            $arguments += @('--job', 'Dry')
        }

        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Write-IcodProgressLine (
            'Starting prepared-input benchmark {0}/{1}: {2} ({3}).' -f
                $pass,
                $effectivePasses,
                $passLabel,
                $candidateCommit.Substring(0, 7)
        )
        & dotnet @arguments | Out-Host
        $exitCode = $LASTEXITCODE
        $watch.Stop()
        if (0 -ne $exitCode) {
            throw "Prepared-input benchmark $passLabel failed."
        }

        & $validator -ArtifactDirectory $passOutput
        if (0 -ne $LASTEXITCODE) {
            throw "Prepared-input benchmark artifact validation failed for $passLabel."
        }

        $sequence.Add(
            [PSCustomObject]@{
                Pass = $pass
                Output = $passLabel
                Commit = $candidateCommit
                ElapsedSeconds = [Math]::Round($watch.Elapsed.TotalSeconds, 3)
            }
        )

        if (
            0 -lt $effectiveCooldownSeconds -and
            $pass -lt $effectivePasses
        ) {
            Write-IcodProgressLine "Cooling down for $effectiveCooldownSeconds seconds."
            Start-Sleep -Seconds $effectiveCooldownSeconds
        }
    }

    $repoParent = Split-Path -Parent $repoRoot
    $defaultInventoryPath = Join-Path $repoParent 'Icod.Grep/hardware_inventory.txt'
    $inventoryHash = if (Test-Path -LiteralPath $defaultInventoryPath -PathType Leaf) {
        (Get-FileHash -LiteralPath $defaultInventoryPath -Algorithm SHA256).Hash.ToLowerInvariant()
    } else {
        $null
    }
    $dotnetVersion = (& dotnet --version).Trim()
    if (0 -ne $LASTEXITCODE) {
        throw 'Unable to record the .NET SDK version.'
    }

    $comparison = [PSCustomObject]@{
        SchemaVersion = 1
        CandidateCommit = $candidateCommit
        BenchmarkMode = 'InProcess'
        BenchmarkClass = 'PreparedRegexInputBenchmarks'
        Smoke = [bool]$Smoke
        Passes = $effectivePasses
        CooldownSeconds = $effectiveCooldownSeconds
        Sequence = $sequence.ToArray()
        HardwareInventorySha256 = $inventoryHash
        DotNetSdkVersion = $dotnetVersion
        CollectedUtc = [DateTimeOffset]::UtcNow.ToString('O')
    } | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText(
        (Join-Path $outputRoot 'comparison.json'),
        $comparison,
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-IcodProgressLine "Prepared-input comparison complete. Results: $outputRoot"
} finally {
    Pop-Location
}
