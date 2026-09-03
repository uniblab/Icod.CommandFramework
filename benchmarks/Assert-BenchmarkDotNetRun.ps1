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

$logs = @(
    Get-ChildItem -LiteralPath $ArtifactDirectory -Recurse -Filter '*.log' -File |
        Sort-Object FullName
)
if (0 -eq $logs.Count) {
    throw "BenchmarkDotNet produced no log files in '$ArtifactDirectory'."
}

$positiveExecution = $false
foreach ($log in $logs) {
    $text = [System.IO.File]::ReadAllText($log.FullName)
    foreach ($failureMarker in @(
        'BenchmarkDotNet has failed to build the auto-generated boilerplate code.',
        'Benchmarks with issues:',
        '// Build Error:'
    )) {
        if ($text.Contains($failureMarker, [System.StringComparison]::Ordinal)) {
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
$reports = if (Test-Path -LiteralPath $resultDirectory -PathType Container) {
    @(
        Get-ChildItem -LiteralPath $resultDirectory -Filter '*-report.csv' -File |
            Sort-Object Name
    )
} else {
    @()
}
if (0 -eq $reports.Count) {
    throw "BenchmarkDotNet produced no CSV reports in '$resultDirectory'."
}

$measuredRow = $false
foreach ($report in $reports) {
    $rows = @(Import-Csv -LiteralPath $report.FullName)
    foreach ($row in $rows) {
        $meanProperty = $row.PSObject.Properties['Mean']
        if ($null -eq $meanProperty) {
            continue
        }
        $mean = [string]$meanProperty.Value
        if (
            -not [string]::IsNullOrWhiteSpace($mean)
            -and $mean -ne 'NA'
            -and $mean -ne '?'
        ) {
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
