param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not [System.IO.Path]::IsPathRooted($ArtifactDirectory)) {
    $ArtifactDirectory = [System.IO.Path]::GetFullPath($ArtifactDirectory)
}
if (-not (Test-Path -LiteralPath $ArtifactDirectory -PathType Container)) {
    throw "BenchmarkDotNet artifact directory '$ArtifactDirectory' does not exist."
}

[object[]]$logs = @(
    Get-ChildItem -LiteralPath $ArtifactDirectory -Recurse -Filter '*.log' -File |
        Sort-Object FullName
)
if (0 -eq $logs.Length) {
    throw "BenchmarkDotNet produced no log files in '$ArtifactDirectory'."
}

$positiveExecution = $false
foreach ($log in $logs) {
    $text = [System.IO.File]::ReadAllText($log.FullName)
    foreach ($failureMarker in @(
        'BenchmarkDotNet has failed to build the auto-generated boilerplate code.',
        'Benchmarks with issues:',
        '// Build Error:',
        'Declaring type must be unsealed.'
    )) {
        if (0 -le $text.IndexOf($failureMarker, [System.StringComparison]::Ordinal)) {
            throw "BenchmarkDotNet failure marker '$failureMarker' was found in '$($log.FullName)'."
        }
    }

    if ($text -match 'executed benchmarks:\s*0(?:\D|$)') {
        throw "BenchmarkDotNet reported zero executed benchmarks in '$($log.FullName)'."
    }
    if ($text -match 'executed benchmarks:\s*[1-9][0-9]*') {
        $positiveExecution = $true
    }
}
if (-not $positiveExecution) {
    throw 'BenchmarkDotNet logs did not confirm that any benchmark executed.'
}

$resultDirectory = Join-Path $ArtifactDirectory 'results'
[object[]]$reports = @()
if (Test-Path -LiteralPath $resultDirectory -PathType Container) {
    $reports = @(
        Get-ChildItem -LiteralPath $resultDirectory -Filter '*-report.csv' -File |
            Sort-Object Name
    )
}
if (0 -eq $reports.Length) {
    throw "BenchmarkDotNet produced no CSV reports in '$resultDirectory'."
}

$measuredRow = $false
foreach ($report in $reports) {
    [object[]]$rows = @(Import-Csv -LiteralPath $report.FullName)
    foreach ($row in $rows) {
        $meanProperty = $row.PSObject.Properties['Mean']
        if ($null -eq $meanProperty) {
            continue
        }
        $mean = [string]$meanProperty.Value
        $hasMeasuredMean = (
            (-not [string]::IsNullOrWhiteSpace($mean)) -and
            ($mean -ne 'NA') -and
            ($mean -ne '?')
        )
        if ($hasMeasuredMean) {
            $measuredRow = $true
            break
        }
    }
    if ($measuredRow) {
        break
    }
}
if (-not $measuredRow) {
    throw 'BenchmarkDotNet reports contain no measured result rows.'
}

Write-Host "BenchmarkDotNet artifact validation passed: $ArtifactDirectory"
