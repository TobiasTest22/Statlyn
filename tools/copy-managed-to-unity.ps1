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
    "Statlyn.Data",
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

$packagesRoot = Join-Path $env:USERPROFILE ".nuget\packages"

function Get-LatestPackageVersionPath {
    param([string]$PackageName)

    $packagePath = Join-Path $packagesRoot $PackageName
    if (-not (Test-Path $packagePath)) {
        throw "Missing NuGet package folder: $packagePath"
    }

    return Get-ChildItem -LiteralPath $packagePath -Directory |
        Sort-Object { [version]$_.Name } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

function Copy-Dependency {
    param(
        [string]$PackageName,
        [string]$RelativePath
    )

    $packagePath = Get-LatestPackageVersionPath $PackageName
    $source = Join-Path $packagePath $RelativePath
    if (-not (Test-Path $source)) {
        throw "Missing dependency assembly: $source"
    }

    Copy-Item -LiteralPath $source -Destination $targetDir -Force
}

Copy-Dependency "microsoft.data.sqlite.core" "lib\netstandard2.0\Microsoft.Data.Sqlite.dll"
Copy-Dependency "sqlitepclraw.core" "lib\netstandard2.0\SQLitePCLRaw.core.dll"
Copy-Dependency "sqlitepclraw.bundle_e_sqlite3" "lib\netstandard2.0\SQLitePCLRaw.batteries_v2.dll"
Copy-Dependency "sqlitepclraw.provider.e_sqlite3" "lib\netstandard2.0\SQLitePCLRaw.provider.e_sqlite3.dll"

$nativeDir = Join-Path $targetDir "runtimes\win-x64\native"
New-Item -ItemType Directory -Force -Path $nativeDir | Out-Null
$nativePackage = Get-LatestPackageVersionPath "sqlitepclraw.lib.e_sqlite3"
$nativeSource = Join-Path $nativePackage "runtimes\win-x64\native\e_sqlite3.dll"
if (Test-Path $nativeSource) {
    Copy-Item -LiteralPath $nativeSource -Destination $nativeDir -Force
}
else {
    Write-Warning "SQLite native dependency was not found for win-x64: $nativeSource"
}

Write-Host "Copied Statlyn managed assemblies to $targetDir"
