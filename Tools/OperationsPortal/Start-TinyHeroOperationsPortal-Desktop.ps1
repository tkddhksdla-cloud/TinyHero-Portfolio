param(
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$portalScriptPath = Join-Path $projectRoot "Tools\OperationsPortal\Start-TinyHeroOperationsPortal.ps1"
$jenkinsScriptPath = Join-Path $projectRoot "Tools\CI\Start-TinyHeroJenkins.ps1"
$portalUrl = "http://127.0.0.1:8090"
$jenkinsUrl = "http://127.0.0.1:8081"
$maximumWaitSeconds = 30
$launcherPidPath = Join-Path $projectRoot "Temp\TinyHeroOperationsPortalLauncher.pid"
$jenkinsLauncherPidPath = Join-Path $projectRoot "Temp\TinyHeroJenkinsLauncher.pid"
New-Item -ItemType Directory -Path (Split-Path -Parent $launcherPidPath) -Force | Out-Null
Set-Content -LiteralPath $launcherPidPath -Value $PID

if ((Test-Path -LiteralPath $portalScriptPath -PathType Leaf) -eq $false) {
    Write-Host "TinyHero Operations Portal script was not found." -ForegroundColor Red
    Write-Host $portalScriptPath
    Read-Host "Press Enter to close"
    exit 1
}

if ((Test-Path -LiteralPath $jenkinsScriptPath -PathType Leaf) -eq $false) {
    Write-Host "TinyHero Jenkins script was not found." -ForegroundColor Red
    Write-Host $jenkinsScriptPath
    Read-Host "Press Enter to close"
    exit 1
}

$jenkinsListener = Get-NetTCPConnection -LocalPort 8081 -State Listen -ErrorAction SilentlyContinue

if ($null -eq $jenkinsListener) {
    $logRoot = Join-Path $projectRoot "Temp"
    $jenkinsOutputLogPath = Join-Path $logRoot "TinyHeroJenkins.stdout.log"
    $jenkinsErrorLogPath = Join-Path $logRoot "TinyHeroJenkins.stderr.log"
    $powerShellExecutable = (Get-Process -Id $PID).Path
    New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
    Write-Host "Starting Jenkins in the background: $jenkinsUrl" -ForegroundColor Cyan
    $jenkinsProcess = Start-Process `
        -FilePath $powerShellExecutable `
        -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $jenkinsScriptPath, "-NoBrowser") `
        -WindowStyle Hidden `
        -PassThru `
        -RedirectStandardOutput $jenkinsOutputLogPath `
        -RedirectStandardError $jenkinsErrorLogPath
    Set-Content -LiteralPath $jenkinsLauncherPidPath -Value $jenkinsProcess.Id
}
else {
    Write-Host "Jenkins is already running: $jenkinsUrl" -ForegroundColor Green
}

$existingListener = Get-NetTCPConnection -LocalAddress "127.0.0.1" -LocalPort 8090 -State Listen -ErrorAction SilentlyContinue

if ($null -ne $existingListener) {
    if ($NoBrowser.IsPresent -eq $false) {
        Start-Process $portalUrl
    }

    Write-Host "TinyHero Operations Portal is already running: $portalUrl" -ForegroundColor Green
    Read-Host "Press Enter to close"
    exit 0
}

$browserJob = $null

if ($NoBrowser.IsPresent -eq $false) {
    $browserJob = Start-Job -ScriptBlock {
        param(
            [string]$targetUrl,
            [int]$waitSeconds
        )

        for ($elapsedSeconds = 0; $elapsedSeconds -lt $waitSeconds; $elapsedSeconds++) {
            try {
                $response = Invoke-WebRequest -Uri "$targetUrl/api/status" -TimeoutSec 2 -UseBasicParsing

                if ($response.StatusCode -eq 200) {
                    Start-Process $targetUrl
                    return
                }
            }
            catch {
            }

            Start-Sleep -Seconds 1
        }
    } -ArgumentList $portalUrl, $maximumWaitSeconds
}

Set-Location -LiteralPath $projectRoot
Write-Host "Starting TinyHero Operations Portal..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C in this window to stop the server."
Write-Host ""

try {
    & $portalScriptPath
}
catch {
    Write-Host "TinyHero Operations Portal stopped with an error." -ForegroundColor Red
    Write-Host $_.Exception.Message
}

if ($null -ne $browserJob) {
    Remove-Job -Job $browserJob -Force -ErrorAction SilentlyContinue
}

Remove-Item -LiteralPath $launcherPidPath -Force -ErrorAction SilentlyContinue
Read-Host "The server has stopped. Press Enter to close"
