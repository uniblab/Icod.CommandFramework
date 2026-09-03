[CmdletBinding()]
param(
    [string]$Filter = '*',
    [string]$OutputDirectory = 'artifacts/performance/regex-reference-comparison',
    [ValidateRange(1, 8)]
    [int]$Passes = 2,
    [ValidateRange(0, 600)]
    [int]$CooldownSeconds = 30,
    [switch]$AllowDirty,
    [switch]$Smoke
)

$ErrorActionPreference = 'Stop'
$baselineCommit = '460732c9f0cacb194bc6cd97c71612c492603eb6'
$repoRoot = (git rev-parse --show-toplevel).Trim()
if (0 -ne $LASTEXITCODE -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw 'Unable to resolve the repository root.'
}

function Write-IcodProgressLine {
    param(
        [Parameter(Mandatory)]
        [string]$Message
    )

    Write-Host ('[{0}] {1}' -f [DateTimeOffset]::Now.ToString('HH:mm:ss'), $Message)
}

function Test-IcodGitCommit {
    param(
        [Parameter(Mandatory)]
        [string]$Commit
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'
        $null = & git cat-file -e "$Commit^{commit}" 2>&1
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    return 0 -eq $exitCode
}

function Invoke-IcodGitChecked {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'
        $output = @(& git @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $lines = @(
        $output | ForEach-Object {
            $_.ToString()
        }
    )

    if (0 -ne $exitCode) {
        $detail = $lines -join [Environment]::NewLine
        if ([string]::IsNullOrWhiteSpace($detail)) {
            throw $FailureMessage
        }
        throw ("{0}{1}{2}" -f $FailureMessage, [Environment]::NewLine, $detail)
    }

    foreach ($line in $lines) {
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            Write-Host $line
        }
    }
}

function Invoke-IcodGitIgnoringFailure {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'
        $null = & git @Arguments 2>&1
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

Push-Location $repoRoot
try {
    if (-not $AllowDirty) {
        $dirty = @(git status --porcelain)
        if (0 -ne $LASTEXITCODE) {
            throw 'Unable to inspect repository status.'
        }
        if (0 -lt $dirty.Count) {
            throw 'The authoritative regex comparison requires a clean worktree. Commit/stash changes or use -AllowDirty for an explicitly non-authoritative run.'
        }
    }

    $candidateCommit = (git rev-parse HEAD).Trim()
    if (0 -ne $LASTEXITCODE -or [string]::IsNullOrWhiteSpace($candidateCommit)) {
        throw 'Unable to resolve candidate commit.'
    }

    if (-not (Test-IcodGitCommit -Commit $baselineCommit)) {
        Invoke-IcodGitChecked `
            -Arguments @('fetch', '--quiet', 'origin', $baselineCommit, '--depth=1') `
            -FailureMessage 'Unable to fetch the pinned 2.1.0 baseline commit.'
    }

    $effectivePasses = if ($Smoke) { 1 } else { $Passes }
    $effectiveCooldownSeconds = if ($Smoke) { 0 } else { $CooldownSeconds }
    $totalRuns = 2 * $effectivePasses
    Write-IcodProgressLine "Regex reference comparison starting. Filter='$Filter'; passes=$effectivePasses; runs=$totalRuns; cooldown=${effectiveCooldownSeconds}s."

    $outputRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
    if (Test-Path -LiteralPath $outputRoot) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

    $repoParent = Split-Path -Parent $repoRoot
    $temporaryRoot = Join-Path $repoParent ('Icod.CommandFramework-R2-' + [Guid]::NewGuid().ToString('N'))
    $baselineRoot = Join-Path $temporaryRoot 'baseline-2.1.0'
    $candidateRoot = Join-Path $temporaryRoot 'candidate'
    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null

    Write-IcodProgressLine 'Preparing fresh baseline and candidate worktrees.'
    Invoke-IcodGitChecked `
        -Arguments @('worktree', 'add', '--detach', $baselineRoot, $baselineCommit) `
        -FailureMessage 'Unable to create the pinned 2.1.0 baseline worktree.'
    try {
        Invoke-IcodGitChecked `
            -Arguments @('worktree', 'add', '--detach', $candidateRoot, $candidateCommit) `
            -FailureMessage 'Unable to create the candidate worktree.'
    } catch {
        Invoke-IcodGitIgnoringFailure -Arguments @('worktree', 'remove', '--force', $baselineRoot)
        throw
    }

    try {
        $sourceProject = Join-Path $repoRoot 'benchmarks/RegularExpressions.Benchmarks'
        $baselineProject = Join-Path $baselineRoot 'benchmarks/RegularExpressions.Benchmarks'
        New-Item -ItemType Directory -Path $baselineProject -Force | Out-Null
        Get-ChildItem -LiteralPath $sourceProject -Force | Where-Object {
            $_.Name -notin @('bin', 'obj', 'BenchmarkDotNet.Artifacts')
        } | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $baselineProject -Recurse -Force
        }

        function Initialize-IcodRegexBenchmarkVariant {
            param(
                [Parameter(Mandatory)]
                [string]$Root,
                [Parameter(Mandatory)]
                [string]$Label
            )

            $watch = [System.Diagnostics.Stopwatch]::StartNew()
            Write-IcodProgressLine "Restoring/building $Label."
            Push-Location $Root
            try {
                dotnet restore benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj | Out-Host
                if (0 -ne $LASTEXITCODE) {
                    throw "$Label benchmark restore failed."
                }
                dotnet build benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Release --no-restore | Out-Host
                if (0 -ne $LASTEXITCODE) {
                    throw "$Label benchmark build failed."
                }
            } finally {
                Pop-Location
                $watch.Stop()
            }
            Write-IcodProgressLine ("$Label ready in {0:n1}s." -f $watch.Elapsed.TotalSeconds)
        }

        function Invoke-IcodRegexBenchmarkVariant {
            param(
                [Parameter(Mandatory)]
                [string]$Root,
                [Parameter(Mandatory)]
                [string]$Label,
                [Parameter(Mandatory)]
                [string]$Commit,
                [Parameter(Mandatory)]
                [int]$Pass,
                [Parameter(Mandatory)]
                [int]$RunNumber,
                [Parameter(Mandatory)]
                [int]$RunCount
            )

            $passLabel = "$Label-pass-$Pass"
            $variantOutput = Join-Path $outputRoot $passLabel
            New-Item -ItemType Directory -Path $variantOutput -Force | Out-Null
            $watch = [System.Diagnostics.Stopwatch]::StartNew()
            Write-IcodProgressLine "Starting regex benchmark run $RunNumber/${RunCount}: $passLabel ($($Commit.Substring(0, 7)))."

            $previousSource = $env:ICOD_REGEX_BENCHMARK_SOURCE
            $previousLabel = $env:ICOD_REGEX_BENCHMARK_LABEL
            $previousCommit = $env:ICOD_REGEX_BENCHMARK_COMMIT
            $previousMetadata = $env:ICOD_REGEX_BENCHMARK_METADATA_PATH
            try {
                $env:ICOD_REGEX_BENCHMARK_SOURCE = if ($Smoke) { 'OrchestrationSmoke' } else { 'PhysicalReference' }
                $env:ICOD_REGEX_BENCHMARK_LABEL = $passLabel
                $env:ICOD_REGEX_BENCHMARK_COMMIT = $Commit
                $env:ICOD_REGEX_BENCHMARK_METADATA_PATH = Join-Path $variantOutput 'metadata.json'

                Push-Location $Root
                try {
                    $bdnArtifacts = Join-Path $Root 'BenchmarkDotNet.Artifacts'
                    if (Test-Path -LiteralPath $bdnArtifacts) {
                        Remove-Item -LiteralPath $bdnArtifacts -Recurse -Force
                    }

                    if ($Smoke) {
                        dotnet run --project benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Release --no-build --no-restore -- --metadata $env:ICOD_REGEX_BENCHMARK_METADATA_PATH | Out-Host
                        if (0 -ne $LASTEXITCODE) {
                            throw "$passLabel metadata smoke failed."
                        }
                        dotnet run --project benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Release --no-build --no-restore -- --smoke | Out-Host
                    } else {
                        dotnet run --project benchmarks/RegularExpressions.Benchmarks/Icod.CommandFramework.RegularExpressions.Benchmarks.csproj -c Release --no-build --no-restore -- --filter $Filter | Out-Host
                    }
                    if (0 -ne $LASTEXITCODE) {
                        throw "$passLabel benchmark run failed."
                    }

                    if (Test-Path -LiteralPath $bdnArtifacts) {
                        Copy-Item -LiteralPath $bdnArtifacts -Destination (Join-Path $variantOutput 'BenchmarkDotNet.Artifacts') -Recurse -Force
                    }
                } finally {
                    Pop-Location
                }
            } finally {
                $env:ICOD_REGEX_BENCHMARK_SOURCE = $previousSource
                $env:ICOD_REGEX_BENCHMARK_LABEL = $previousLabel
                $env:ICOD_REGEX_BENCHMARK_COMMIT = $previousCommit
                $env:ICOD_REGEX_BENCHMARK_METADATA_PATH = $previousMetadata
                $watch.Stop()
            }

            Write-IcodProgressLine ("Completed run $RunNumber/${RunCount}: $passLabel in {0:n1} minutes." -f $watch.Elapsed.TotalMinutes)
            return [PSCustomObject]@{
                Label = $Label
                Pass = $Pass
                Output = $passLabel
                Commit = $Commit
                ElapsedSeconds = [Math]::Round($watch.Elapsed.TotalSeconds, 3)
            }
        }

        Initialize-IcodRegexBenchmarkVariant -Root $baselineRoot -Label 'baseline-2.1.0'
        Initialize-IcodRegexBenchmarkVariant -Root $candidateRoot -Label 'candidate'

        $sequence = New-Object System.Collections.Generic.List[object]
        $runNumber = 0
        for ($pass = 1; $pass -le $effectivePasses; $pass++) {
            if (0 -eq ($pass % 2)) {
                $variants = @(
                    [PSCustomObject]@{ Root = $candidateRoot; Label = 'candidate'; Commit = $candidateCommit },
                    [PSCustomObject]@{ Root = $baselineRoot; Label = 'baseline-2.1.0'; Commit = $baselineCommit }
                )
            } else {
                $variants = @(
                    [PSCustomObject]@{ Root = $baselineRoot; Label = 'baseline-2.1.0'; Commit = $baselineCommit },
                    [PSCustomObject]@{ Root = $candidateRoot; Label = 'candidate'; Commit = $candidateCommit }
                )
            }

            foreach ($variant in $variants) {
                $runNumber++
                $record = Invoke-IcodRegexBenchmarkVariant `
                    -Root $variant.Root `
                    -Label $variant.Label `
                    -Commit $variant.Commit `
                    -Pass $pass `
                    -RunNumber $runNumber `
                    -RunCount $totalRuns
                $sequence.Add( $record )

                if (0 -lt $effectiveCooldownSeconds -and $runNumber -lt $totalRuns) {
                    Write-IcodProgressLine "Cooling down for $effectiveCooldownSeconds seconds."
                    Start-Sleep -Seconds $effectiveCooldownSeconds
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
            BaselineCommit = $baselineCommit
            CandidateCommit = $candidateCommit
            Filter = $Filter
            Smoke = [bool]$Smoke
            Passes = $effectivePasses
            CooldownSeconds = $effectiveCooldownSeconds
            Sequence = $sequence.ToArray()
            HardwareInventorySha256 = $inventoryHash
            CollectedUtc = [DateTimeOffset]::UtcNow.ToString('O')
        } | ConvertTo-Json -Depth 6
        [System.IO.File]::WriteAllText(
            (Join-Path $outputRoot 'comparison.json'),
            $comparison,
            [System.Text.UTF8Encoding]::new($false)
        )

        Write-IcodProgressLine "Regex reference comparison complete. Results: $outputRoot"
    } finally {
        Write-IcodProgressLine 'Removing temporary worktrees.'
        Invoke-IcodGitIgnoringFailure -Arguments @('worktree', 'remove', '--force', $candidateRoot)
        Invoke-IcodGitIgnoringFailure -Arguments @('worktree', 'remove', '--force', $baselineRoot)
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
} finally {
    Pop-Location
}
