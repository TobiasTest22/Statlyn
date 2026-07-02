param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$targetDir = Join-Path $repoRoot "Statlyn.UnityApp\Assets\Plugins\Managed\Statlyn"
$projects = @(
    "Statlyn.Core",
    "Statlyn.DataProviders",
    "Statlyn.Scouting",
    "Statlyn.Analytics",
    "Statlyn.UI"
)

dotnet build (Join-Path $repoRoot "Statlyn.sln") -c $Configuration

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

foreach ($project in $projects) {
    $source = Join-Path $repoRoot "$project\bin\$Configuration\netstandard2.1\$project.dll"
    if (-not (Test-Path $source)) {
        throw "Missing managed assembly: $source"
    }

    Copy-Item -LiteralPath $source -Destination $targetDir -Force
}

Write-Host "Copied Statlyn managed assemblies to $targetDir"
