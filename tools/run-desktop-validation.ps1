param(
    [switch]$SkipNpmInstall,
    [switch]$SkipTauriBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$desktopRoot = Join-Path $repoRoot "Statlyn.Desktop"
$apiLog = Join-Path $repoRoot "statlyn-api-validation.log"
$apiErrorLog = Join-Path $repoRoot "statlyn-api-validation.err.log"
$apiProcess = $null
$apiStarted = $false

function Invoke-ValidationStep {
    param(
        [string]$Name,
        [scriptblock]$Script
    )

    Write-Host "[Statlyn] $Name"
    $global:LASTEXITCODE = 0
    & $Script
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }

    Write-Host "[Statlyn] PASS: $Name"
}

function Invoke-InDirectory {
    param(
        [string]$Path,
        [scriptblock]$Script
    )

    Push-Location $Path
    try {
        & $Script
    }
    finally {
        Pop-Location
    }
}

try {
    Invoke-ValidationStep "dotnet build" {
        dotnet build (Join-Path $repoRoot "Statlyn.sln")
    }

    Invoke-ValidationStep "dotnet test" {
        dotnet test (Join-Path $repoRoot "Statlyn.sln")
    }

    Invoke-ValidationStep "native read-only scan" {
        & (Join-Path $repoRoot "tools/check-native-readonly.ps1")
    }

    Invoke-ValidationStep "tracked JSON validation" {
        $jsonCheck = @'
const { execSync } = require("child_process");
const fs = require("fs");
const files = execSync("git ls-files *.json", { encoding: "utf8" }).trim().split(/\r?\n/).filter(Boolean);
let failures = 0;
for (const file of files) {
  try {
    JSON.parse(fs.readFileSync(file, "utf8").replace(/^\uFEFF/, ""));
  } catch (error) {
    failures += 1;
    console.error(`${file}: ${error.message}`);
  }
}
console.log(`trackedJsonFiles=${files.length}`);
if (failures > 0) {
  process.exit(1);
}
'@
        $jsonCheckPath = Join-Path ([System.IO.Path]::GetTempPath()) ("statlyn-json-check-" + [System.Guid]::NewGuid().ToString("N") + ".js")
        Set-Content -LiteralPath $jsonCheckPath -Value $jsonCheck -Encoding UTF8
        try {
            Invoke-InDirectory $repoRoot {
                node $jsonCheckPath
            }
        }
        finally {
            if (Test-Path $jsonCheckPath) {
                Remove-Item -LiteralPath $jsonCheckPath -Force
            }
        }
    }

    Invoke-ValidationStep "start Statlyn.Api and check health" {
        $existingApiListener = Get-NetTCPConnection -LocalPort 5118 -State Listen -ErrorAction SilentlyContinue
        if ($existingApiListener) {
            throw "Port 5118 is already in use. Stop the existing Statlyn.Api process before running validation."
        }

        if (Test-Path $apiLog) {
            Remove-Item -LiteralPath $apiLog -Force
        }
        if (Test-Path $apiErrorLog) {
            Remove-Item -LiteralPath $apiErrorLog -Force
        }

        $apiProcess = Start-Process -FilePath "dotnet" `
            -ArgumentList @("run", "--project", (Join-Path $repoRoot "Statlyn.Api/Statlyn.Api.csproj"), "--urls", "http://localhost:5118") `
            -RedirectStandardOutput $apiLog `
            -RedirectStandardError $apiErrorLog `
            -WindowStyle Hidden `
            -PassThru
        $script:apiStarted = $true

        $healthy = $false
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            Start-Sleep -Seconds 1
            try {
                $health = Invoke-RestMethod -Uri "http://127.0.0.1:5118/health" -TimeoutSec 2
                if ($health.status -eq "ok" -and -not $health.isFm26Supported) {
                    $healthy = $true
                    break
                }
            }
            catch {
            }
        }

        if (-not $healthy) {
            if (Test-Path $apiLog) {
                Get-Content -LiteralPath $apiLog -Tail 80
            }
            if (Test-Path $apiErrorLog) {
                Get-Content -LiteralPath $apiErrorLog -Tail 80
            }

            throw "Statlyn.Api did not return a safe health response."
        }
    }

    Invoke-ValidationStep "npm install when needed" {
        Invoke-InDirectory $desktopRoot {
            if (-not $SkipNpmInstall -and -not (Test-Path "node_modules")) {
                npm install
            }
            else {
                Write-Host "[Statlyn] node_modules present or npm install skipped."
            }
        }
    }

    Invoke-ValidationStep "desktop check" {
        Invoke-InDirectory $desktopRoot {
            npm run check
        }
    }

    if (-not $SkipTauriBuild) {
        Invoke-ValidationStep "tauri build" {
            Invoke-InDirectory $desktopRoot {
                npm run tauri:build
            }
        }
    }
    else {
        Write-Host "[Statlyn] SKIP: tauri build"
    }
}
finally {
    if ($apiStarted) {
        Get-NetTCPConnection -LocalPort 5118 -State Listen -ErrorAction SilentlyContinue | ForEach-Object {
            Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }

    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }
}
