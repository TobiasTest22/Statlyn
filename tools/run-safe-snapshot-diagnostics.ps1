param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiLog = Join-Path $repoRoot "statlyn-safe-snapshot-api.log"
$apiErrorLog = Join-Path $repoRoot "statlyn-safe-snapshot-api.err.log"
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

function Invoke-SnapshotStep {
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
        Invoke-SnapshotStep "dotnet build" {
            dotnet build (Join-Path $repoRoot "Statlyn.sln")
        }
    }

    Invoke-SnapshotStep "native read-only scan" {
        & (Join-Path $repoRoot "tools/check-native-readonly.ps1")
    }

    Invoke-SnapshotStep "memory-map registry validation" {
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

    Invoke-SnapshotStep "safe FM26 snapshot diagnostics" {
        $health = $null
        $connector = $null
        $memoryMaps = $null
        $snapshot = $null

        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            Start-Sleep -Seconds 1
            try {
                $health = Invoke-RestMethod -Uri "http://127.0.0.1:5118/health" -TimeoutSec 2
                $connector = Invoke-RestMethod -Uri "http://127.0.0.1:5118/connector/status" -TimeoutSec 2
                $memoryMaps = Invoke-RestMethod -Uri "http://127.0.0.1:5118/diagnostics/memory-maps" -TimeoutSec 2
                $snapshot = Invoke-RestMethod -Uri "http://127.0.0.1:5118/diagnostics/fm26/snapshot" -TimeoutSec 2
                if ($health.status -eq "ok" -and $connector -ne $null -and $memoryMaps -ne $null -and $snapshot -ne $null) {
                    break
                }
            }
            catch {
            }
        }

        if ($health -eq $null -or $connector -eq $null -or $memoryMaps -eq $null -or $snapshot -eq $null) {
            if (Test-Path $apiLog) {
                Get-Content -LiteralPath $apiLog -Tail 60
            }
            if (Test-Path $apiErrorLog) {
                Get-Content -LiteralPath $apiErrorLog -Tail 60
            }

            throw "Statlyn.Api did not return safe FM26 snapshot diagnostics."
        }

        if ($health.isFm26Supported -or $connector.isFm26Supported -or $snapshot.isFm26Supported -or $snapshot.isLiveReadingAvailable) {
            throw "FM26 support or live reading was reported before a validated reader exists."
        }

        Write-Host ("[Statlyn] Health: " + $health.status)
        Write-Host ("[Statlyn] Connector status: " + $snapshot.connectorStatus)
        Write-Host ("[Statlyn] Process status: " + $snapshot.fmProcessStatus)
        Write-Host ("[Statlyn] Read-only status: " + $snapshot.readOnlyStatus)
        Write-Host ("[Statlyn] Map registry: " + $snapshot.mapRegistryStatus)
        Write-Host ("[Statlyn] Maps found: " + $snapshot.mapsFound)
        Write-Host ("[Statlyn] Validated maps: " + $snapshot.validatedMaps)
        Write-Host ("[Statlyn] Template maps: " + $snapshot.templateMaps)
        Write-Host ("[Statlyn] Snapshot status: " + $snapshot.snapshotStatus)
        Write-Host ("[Statlyn] Blocking gate: " + $(if ([string]::IsNullOrWhiteSpace($snapshot.blockingGate)) { "None" } else { $snapshot.blockingGate }))
        Write-Host ("[Statlyn] Next action: " + $snapshot.nextAction)
        Write-Host "[Statlyn] Safe snapshot contains diagnostics metadata only. No player data is read."
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
