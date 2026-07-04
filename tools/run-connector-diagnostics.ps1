param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiLog = Join-Path $repoRoot "statlyn-connector-api.log"
$apiErrorLog = Join-Path $repoRoot "statlyn-connector-api.err.log"
$apiProcess = $null
$apiStarted = $false

function Stop-StatlynApiListener {
    $listeners = Get-NetTCPConnection -LocalPort 5118 -State Listen -ErrorAction SilentlyContinue
    foreach ($listener in $listeners) {
        $process = Get-CimInstance Win32_Process -Filter ("ProcessId = " + $listener.OwningProcess) -ErrorAction SilentlyContinue
        if ($process -and ($process.CommandLine -like "*Statlyn.Api*" -or $process.Name -eq "Statlyn.Api.exe")) {
            Write-Host ("[Statlyn] Stopping existing Statlyn.Api listener: " + $listener.OwningProcess)
            Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
}

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
    Stop-StatlynApiListener

    if (-not $SkipBuild) {
        Invoke-ConnectorStep "dotnet build" {
            dotnet build (Join-Path $repoRoot "Statlyn.sln")
        }
    }

    Invoke-ConnectorStep "native read-only scan" {
        & (Join-Path $repoRoot "tools/check-native-readonly.ps1")
    }

    Invoke-ConnectorStep "memory-map registry validation" {
        & (Join-Path $repoRoot "tools/validate-memory-maps.ps1")
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
    $apiStarted = $true

    Invoke-ConnectorStep "API health and FM26 diagnostics" {
        $health = $null
        $connector = $null
        $fm26Diagnostics = $null
        $memoryMaps = $null
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            Start-Sleep -Seconds 1
            try {
                $health = Invoke-RestMethod -Uri "http://127.0.0.1:5118/health" -TimeoutSec 2
                $connector = Invoke-RestMethod -Uri "http://127.0.0.1:5118/connector/status" -TimeoutSec 2
                $fm26Diagnostics = Invoke-RestMethod -Uri "http://127.0.0.1:5118/diagnostics/fm26" -TimeoutSec 2
                $memoryMaps = Invoke-RestMethod -Uri "http://127.0.0.1:5118/diagnostics/memory-maps" -TimeoutSec 2
                if ($health.status -eq "ok" -and $connector -ne $null -and $fm26Diagnostics -ne $null -and $memoryMaps -ne $null) {
                    break
                }
            }
            catch {
            }
        }

        if ($health -eq $null -or $connector -eq $null -or $fm26Diagnostics -eq $null -or $memoryMaps -eq $null) {
            if (Test-Path $apiLog) {
                Get-Content -LiteralPath $apiLog -Tail 80
            }
            if (Test-Path $apiErrorLog) {
                Get-Content -LiteralPath $apiErrorLog -Tail 80
            }

            throw "Statlyn.Api did not return FM26 diagnostics."
        }

        if ($health.isFm26Supported -or $connector.isFm26Supported -or $fm26Diagnostics.isFm26Supported) {
            throw "FM26 support was reported before player reading is implemented."
        }

        Write-Host ("[Statlyn] Connector availability: " + $connector.availability)
        Write-Host ("[Statlyn] Platform: " + $(if ($connector.isWindows) { "Windows" } else { "Unsupported" }))
        Write-Host ("[Statlyn] FM detected: " + $connector.isFmProcessDetected)
        Write-Host ("[Statlyn] Detection status: " + $connector.detectionStatus)
        Write-Host ("[Statlyn] Read-only status: " + $connector.readOnlyAccessStatus)
        Write-Host ("[Statlyn] Build support: " + $connector.buildSupportStatus)
        Write-Host ("[Statlyn] Map status: " + $connector.mapSupportStatus)
        Write-Host ("[Statlyn] Map registry: " + $memoryMaps.registryStatus)
        Write-Host ("[Statlyn] Maps found: " + $memoryMaps.mapsFoundCount)
        Write-Host ("[Statlyn] Usable maps: " + $memoryMaps.usableMapsCount)
        Write-Host ("[Statlyn] Template maps: " + $memoryMaps.templateMapsCount)
        Write-Host ("[Statlyn] Invalid maps: " + $memoryMaps.invalidMapsCount)
        Write-Host ("[Statlyn] Support message: " + $connector.supportStatusMessage)
        Write-Host ("[Statlyn] Next action: " + $connector.nextActionSafeMessage)
    }
}
finally {
    if ($apiStarted) {
        if ($apiProcess -and -not $apiProcess.HasExited) {
            Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        }

        Stop-StatlynApiListener
    }
}
