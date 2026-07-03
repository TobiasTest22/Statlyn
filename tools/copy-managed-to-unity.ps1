param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$targetDir = Join-Path $repoRoot "Statlyn.UnityApp\Assets\Plugins\Managed\Statlyn"
$nativePluginDir = Join-Path $repoRoot "Statlyn.UnityApp\Assets\Plugins\x86_64"
$fixtureTargetDir = Join-Path $repoRoot "Statlyn.UnityApp\Assets\StreamingAssets\Statlyn\Fixtures"
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

$copied = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

foreach ($project in $projects) {
    $source = Join-Path $repoRoot "$project\bin\$Configuration\netstandard2.1\$project.dll"
    if (-not (Test-Path $source)) {
        throw "Missing managed assembly: $source"
    }

    Copy-Item -LiteralPath $source -Destination $targetDir -Force
    $copied.Add((Join-Path $targetDir "$project.dll"))
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
    $copied.Add((Join-Path $targetDir (Split-Path $source -Leaf)))
}

Copy-Dependency "microsoft.data.sqlite.core" "lib\netstandard2.0\Microsoft.Data.Sqlite.dll"
Copy-Dependency "sqlitepclraw.core" "lib\netstandard2.0\SQLitePCLRaw.core.dll"
Copy-Dependency "sqlitepclraw.bundle_e_sqlite3" "lib\netstandard2.0\SQLitePCLRaw.batteries_v2.dll"
Copy-Dependency "sqlitepclraw.provider.e_sqlite3" "lib\netstandard2.0\SQLitePCLRaw.provider.e_sqlite3.dll"

$nativeDir = Join-Path $targetDir "runtimes\win-x64\native"
New-Item -ItemType Directory -Force -Path $nativeDir | Out-Null
New-Item -ItemType Directory -Force -Path $nativePluginDir | Out-Null
$nativePackage = Get-LatestPackageVersionPath "sqlitepclraw.lib.e_sqlite3"
$nativeSource = Join-Path $nativePackage "runtimes\win-x64\native\e_sqlite3.dll"
if (Test-Path $nativeSource) {
    $runtimeNative = Join-Path $nativeDir "e_sqlite3.dll"
    $unityNative = Join-Path $nativePluginDir "e_sqlite3.dll"
    Copy-Item -LiteralPath $nativeSource -Destination $runtimeNative -Force
    Copy-Item -LiteralPath $nativeSource -Destination $unityNative -Force
    $copied.Add($runtimeNative)
    $copied.Add($unityNative)
}
else {
    $warnings.Add("SQLite native dependency was not found for win-x64: $nativeSource")
}

$fixtureSource = Join-Path $repoRoot "Statlyn.Tests\Fixtures\players.sample.csv"
if (Test-Path $fixtureSource) {
    New-Item -ItemType Directory -Force -Path $fixtureTargetDir | Out-Null
    $fixtureTarget = Join-Path $fixtureTargetDir "players.sample.csv"
    Copy-Item -LiteralPath $fixtureSource -Destination $fixtureTarget -Force
    $copied.Add($fixtureTarget)
}
else {
    $warnings.Add("Synthetic fixture CSV was not found: $fixtureSource")
}

$requiredFiles = @(
    "Statlyn.Core.dll",
    "Statlyn.DataProviders.dll",
    "Statlyn.Scouting.dll",
    "Statlyn.Analytics.dll",
    "Statlyn.Data.dll",
    "Statlyn.UI.dll",
    "Microsoft.Data.Sqlite.dll",
    "SQLitePCLRaw.core.dll",
    "SQLitePCLRaw.batteries_v2.dll",
    "SQLitePCLRaw.provider.e_sqlite3.dll"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $targetDir $file
    if (-not (Test-Path $path)) {
        throw "Required Unity managed dependency was not copied: $path"
    }
}

$nativePlugin = Join-Path $nativePluginDir "e_sqlite3.dll"
if (-not (Test-Path $nativePlugin)) {
    $warnings.Add("Windows x64 SQLite native plugin was not copied. Unity SQLite runtime may fail until this is resolved: $nativePlugin")
}

Write-Host "Statlyn Unity copy summary"
Write-Host "Managed plugin folder: $targetDir"
Write-Host "Native plugin folder: $nativePluginDir"
Write-Host "Fixture folder: $fixtureTargetDir"
Write-Host "Fixture resolver expected path: Statlyn.UnityApp\Assets\StreamingAssets\Statlyn\Fixtures\players.sample.csv"
Write-Host ("Files copied: " + $copied.Count)
foreach ($item in $copied) {
    Write-Host " - $item"
}

foreach ($warning in $warnings) {
    Write-Warning $warning
}
