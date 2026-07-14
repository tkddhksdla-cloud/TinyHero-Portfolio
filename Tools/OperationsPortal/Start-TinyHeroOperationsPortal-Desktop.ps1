$ErrorActionPreference = "Stop"

$projectRoot = "C:\Path\To\TinyHero"
$portalScriptPath = Join-Path $projectRoot "Tools\OperationsPortal\Start-TinyHeroOperationsPortal.ps1"
$portalUrl = "http://127.0.0.1:8090"
$maximumWaitSeconds = 30

if ((Test-Path -LiteralPath $portalScriptPath -PathType Leaf) -eq $false) {
    Write-Host "TinyHero Operations Portal script was not found." -ForegroundColor Red
    Write-Host $portalScriptPath
    Read-Host "Press Enter to close"
    exit 1
}

$existingListener = Get-NetTCPConnection -LocalAddress "127.0.0.1" -LocalPort 8090 -State Listen -ErrorAction SilentlyContinue

if ($null -ne $existingListener) {
    Start-Process $portalUrl
    Write-Host "TinyHero Operations Portal is already running: $portalUrl" -ForegroundColor Green
    Read-Host "Press Enter to close"
    exit 0
}

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

Remove-Job -Job $browserJob -Force -ErrorAction SilentlyContinue

Read-Host "The server has stopped. Press Enter to close"
