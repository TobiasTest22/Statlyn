param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiLog = Join-Path $repoRoot "statlyn-connector-api.log"
$apiErrorLog = Join-Path $repoRoot "statlyn-connector-api.err.log"
$apiProcess = $null
$apiStarted = $false

function Invoke-ConnectorStep {
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

try {
    if (-not $SkipBuild) {
        Invoke-ConnectorStep "dotnet build" {
            dotnet build (Join-Path $repoRoot "Statlyn.sln")
        }
    }

    Invoke-ConnectorStep "native read-only scan" {
        & (Join-Path $repoRoot "tools/check-native-readonly.ps1")
    }

    $existingApiListener = Get-NetTCPConnection -LocalPort 5118 -State Listen -ErrorAction SilentlyContinue
    if (-not $existingApiListener) {
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
        $apiStarted = $true
    }
    else {
        Write-Host "[Statlyn] Reusing existing API listener on port 5118."
    }

    Invoke-ConnectorStep "API health and connector status" {
        $health = $null
        $connector = $null
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            Start-Sleep -Seconds 1
            try {
                $health = Invoke-RestMethod -Uri "http://127.0.0.1:5118/health" -TimeoutSec 2
                $connector = Invoke-RestMethod -Uri "http://127.0.0.1:5118/connector/status" -TimeoutSec 2
                if ($health.status -eq "ok" -and $connector -ne $null) {
                    break
                }
            }
            catch {
            }
        }

        if ($health -eq $null -or $connector -eq $null) {
            if (Test-Path $apiLog) {
                Get-Content -LiteralPath $apiLog -Tail 80
            }
            if (Test-Path $apiErrorLog) {
                Get-Content -LiteralPath $apiErrorLog -Tail 80
            }

            throw "Statlyn.Api did not return connector diagnostics."
        }

        if ($health.isFm26Supported -or $connector.isFm26Supported) {
            throw "FM26 support was reported before a validated map exists."
        }

        Write-Host ("[Statlyn] Connector availability: " + $connector.availability)
        Write-Host ("[Statlyn] FM detected: " + $connector.isFmProcessDetected)
        Write-Host ("[Statlyn] Read-only status: " + $connector.readOnlyAccessStatus)
    }
}
finally {
    if ($apiStarted) {
        $startedListener = Get-NetTCPConnection -LocalPort 5118 -State Listen -ErrorAction SilentlyContinue
        foreach ($listener in $startedListener) {
            Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
        }

        if ($apiProcess -and -not $apiProcess.HasExited) {
            Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }
}
