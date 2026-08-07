param(
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

$javaExe = "C:\Program Files\Eclipse Adoptium\jdk-21.0.11.10-hotspot\bin\java.exe"
$jenkinsWar = Join-Path $env:USERPROFILE "Downloads\jenkins.war"
$parameterSyncScript = Join-Path $PSScriptRoot "Sync-TinyHeroJenkinsJobParameters.ps1"
$httpPort = 8081
$jenkinsUrl = "http://127.0.0.1:$httpPort"

$existingListener = Get-NetTCPConnection -LocalPort $httpPort -State Listen -ErrorAction SilentlyContinue

if ($null -ne $existingListener) {
    Write-Host "Jenkins is already running: $jenkinsUrl"

    if ($NoBrowser.IsPresent -eq $false) {
        Start-Process $jenkinsUrl
    }

    return
}

if ((Test-Path -LiteralPath $javaExe) -eq $false) {
    throw "Java 21 executable not found. Path: $javaExe"
}

if ((Test-Path -LiteralPath $jenkinsWar) -eq $false) {
    throw "jenkins.war not found. Path: $jenkinsWar"
}

if (Test-Path -LiteralPath $parameterSyncScript -PathType Leaf) {
    & $parameterSyncScript
}

Write-Host "Starting Jenkins..."
Write-Host "URL: $jenkinsUrl"
Write-Host "WAR: $jenkinsWar"
Write-Host ""
Write-Host "Keep this PowerShell window open while using Jenkins."
Write-Host ""

if ($NoBrowser.IsPresent -eq $false) {
    Start-Job -ScriptBlock {
        Start-Sleep -Seconds 8
        Start-Process "http://127.0.0.1:8081"
    } | Out-Null
}

& $javaExe -jar $jenkinsWar --httpPort=$httpPort
