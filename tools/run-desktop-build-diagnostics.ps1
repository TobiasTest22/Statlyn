param(
    [switch]$SkipNpmInstall,
    [switch]$SkipTauriBuild,
    [switch]$LowMemory,
    [switch]$NoBundle,
    [switch]$VerboseLogs
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$desktopRoot = Join-Path $repoRoot "Statlyn.Desktop"
$logPath = Join-Path $repoRoot "statlyn-desktop-build-diagnostics.log"

if (Test-Path $logPath) {
    Remove-Item -LiteralPath $logPath -Force
}

function Write-Statlyn {
    param([string]$Message)
    $line = "[Statlyn] $Message"
    Write-Host $line
    Add-Content -LiteralPath $logPath -Value $line
}

function Get-FailureClassification {
    param(
        [string]$Text,
        [string]$DefaultClassification
    )

    if ($Text -match "(?i)out of memory|LLVM ERROR: out of memory|allocation failed|not enough memory") {
        return "out-of-memory failure"
    }

    if ($Text -match "(?i)rustup|rustc.+not recognized|cargo.+not recognized|no default toolchain") {
        return "missing Rust toolchain"
    }

    if ($Text -match "(?i)WebView2|webview|Microsoft Edge") {
        return "missing WebView2/Windows dependency"
    }

    if ($Text -match "(?i)bundle|bundling|msi|nsis|wix|signing") {
        return "installer bundling failure"
    }

    if ($Text -match "(?i)rustc|cargo|link\.exe|error\[E[0-9]+\]") {
        return "Rust compile failure"
    }

    if ($Text -match "(?i)tsc|vite|typescript|eslint|npm ERR!") {
        return "frontend failure"
    }

    return $DefaultClassification
}

function Invoke-DiagnosticStep {
    param(
        [string]$Name,
        [string]$DefaultClassification,
        [scriptblock]$Script
    )

    Write-Statlyn "START: $Name"
    $global:LASTEXITCODE = 0
    $output = & $Script 2>&1
    $text = ($output | Out-String).TrimEnd()
    if ($text.Length -gt 0) {
        $text | Tee-Object -FilePath $logPath -Append
    }

    if ($LASTEXITCODE -ne 0) {
        $classification = Get-FailureClassification -Text $text -DefaultClassification $DefaultClassification
        Write-Statlyn "FAIL: $Name"
        Write-Statlyn "CLASSIFICATION: $classification"
        throw "$Name failed with exit code $LASTEXITCODE. Classification: $classification. See $logPath."
    }

    Write-Statlyn "PASS: $Name"
}

function Invoke-InDesktop {
    param([scriptblock]$Script)

    Push-Location $desktopRoot
    try {
        & $Script
    }
    finally {
        Pop-Location
    }
}

function Write-ToolVersion {
    param(
        [string]$Name,
        [scriptblock]$Script
    )

    try {
        $global:LASTEXITCODE = 0
        $output = & $Script 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Statlyn "${Name}: $($output | Select-Object -First 1)"
        }
        else {
            Write-Statlyn "${Name}: unavailable"
        }
    }
    catch {
        Write-Statlyn "${Name}: unavailable"
    }
}

Write-Statlyn "Desktop build diagnostics"
Write-Statlyn "Repository: $repoRoot"

try {
    $os = Get-CimInstance Win32_OperatingSystem
    $totalMb = [math]::Round($os.TotalVisibleMemorySize / 1024)
    $freeMb = [math]::Round($os.FreePhysicalMemory / 1024)
    Write-Statlyn "Memory: total ${totalMb} MB, free ${freeMb} MB"
}
catch {
    Write-Statlyn "Memory: unavailable"
}

Write-ToolVersion "dotnet" { dotnet --version }
Write-ToolVersion "node" { node --version }
Write-ToolVersion "npm" { npm --version }
Write-ToolVersion "rustc" { rustc --version }
Write-ToolVersion "cargo" { cargo --version }
Write-ToolVersion "tauri" { Invoke-InDesktop { npx tauri --version } }

if (-not $SkipNpmInstall) {
    Invoke-DiagnosticStep "npm install when needed" "frontend failure" {
        Invoke-InDesktop {
            if (Test-Path "node_modules") {
                Write-Output "node_modules already present."
            }
            else {
                npm install
            }
        }
    }
}
else {
    Write-Statlyn "SKIP: npm install"
}

Invoke-DiagnosticStep "desktop check" "frontend failure" {
    Invoke-InDesktop { npm run check }
}

Invoke-DiagnosticStep "desktop production build" "frontend failure" {
    Invoke-InDesktop { npm run build }
}

if ($SkipTauriBuild) {
    Write-Statlyn "SKIP: tauri build"
    return
}

$previousCargoBuildJobs = $env:CARGO_BUILD_JOBS
$previousCargoIncremental = $env:CARGO_INCREMENTAL

try {
    if ($LowMemory) {
        $env:CARGO_BUILD_JOBS = "1"
        $env:CARGO_INCREMENTAL = "0"
        Write-Statlyn "Low-memory mode: CARGO_BUILD_JOBS=1, CARGO_INCREMENTAL=0"
    }

    $tauriArguments = @("tauri", "build")
    if ($NoBundle) {
        $tauriArguments += "--no-bundle"
        Write-Statlyn "No-bundle mode: installer bundling will be skipped."
    }

    if ($VerboseLogs) {
        $tauriArguments += "--verbose"
    }

    Invoke-DiagnosticStep "tauri build" "Rust compile failure" {
        Invoke-InDesktop { npx @tauriArguments }
    }
}
finally {
    $env:CARGO_BUILD_JOBS = $previousCargoBuildJobs
    $env:CARGO_INCREMENTAL = $previousCargoIncremental
}
