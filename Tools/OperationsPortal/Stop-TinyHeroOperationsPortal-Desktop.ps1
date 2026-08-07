$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$serverPortArray = @(8081, 8082, 8090)
$allowedProcessNameArray = @("java", "dotnet", "TinyHero.OperationsPortal")
$launcherPidPathArray = @(
    (Join-Path $projectRoot "Temp\TinyHeroOperationsPortalLauncher.pid"),
    (Join-Path $projectRoot "Temp\TinyHeroJenkinsLauncher.pid")
)
$stoppedProcessIdSet = [System.Collections.Generic.HashSet[int]]::new()

Write-Host "Stopping TinyHero local operations services..." -ForegroundColor Cyan

foreach ($serverPort in $serverPortArray) {
    $listenerArray = @(Get-NetTCPConnection -LocalPort $serverPort -State Listen -ErrorAction SilentlyContinue)

    foreach ($listener in $listenerArray) {
        $processId = [int]$listener.OwningProcess

        if ($stoppedProcessIdSet.Contains($processId)) {
            continue
        }

        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue

        if ($null -eq $process -or $process.ProcessName -notin $allowedProcessNameArray) {
            Write-Warning "Port $serverPort is used by an unexpected process. It was not stopped. PID: $processId"
            continue
        }

        Stop-Process -Id $processId -Force
        $stoppedProcessIdSet.Add($processId) | Out-Null
        Write-Host "Stopped $($process.ProcessName) on port $serverPort. PID: $processId" -ForegroundColor Green
    }
}

foreach ($launcherPidPath in $launcherPidPathArray) {
    if ((Test-Path -LiteralPath $launcherPidPath -PathType Leaf) -eq $false) {
        continue
    }

    $launcherProcessIdText = Get-Content -LiteralPath $launcherPidPath -Raw
    $launcherProcessId = 0

    if ([int]::TryParse($launcherProcessIdText.Trim(), [ref]$launcherProcessId)) {
        $launcherProcess = Get-Process -Id $launcherProcessId -ErrorAction SilentlyContinue

        if ($null -ne $launcherProcess -and $launcherProcess.ProcessName -in @("powershell", "pwsh")) {
            Stop-Process -Id $launcherProcessId -Force
            Write-Host "Stopped launcher process. PID: $launcherProcessId" -ForegroundColor Green
        }
    }

    Remove-Item -LiteralPath $launcherPidPath -Force -ErrorAction SilentlyContinue
}

if ($stoppedProcessIdSet.Count -eq 0) {
    Write-Host "TinyHero local operations services are not running." -ForegroundColor Yellow
}

Write-Host "TinyHero local operations services have been stopped." -ForegroundColor Cyan
Start-Sleep -Seconds 2
