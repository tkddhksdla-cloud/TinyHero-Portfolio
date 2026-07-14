param(
    [Parameter(Mandatory = $true)]
    [string]$ContentStatePath,
    [string]$UnityExe = $env:UNITY_EXE,
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot/../..").Path,
    [string]$PublishPath = "PublishedContent",
    [string]$LocalServerPath = "",
    [string]$LogFile = "Logs/TinyHeroContentUpdate.log"
)

$ErrorActionPreference = "Stop"
$unityProcessScriptPath = Join-Path $PSScriptRoot "../CI/Invoke-TinyHeroUnityProcess.ps1"
. $unityProcessScriptPath

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe"
}

if ((Test-Path -LiteralPath $UnityExe) -eq $false) {
    throw "Unity executable not found. Path: $UnityExe"
}

$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedContentStatePath = (Resolve-Path -LiteralPath $ContentStatePath).Path
$resolvedLogFile = Join-Path $resolvedProjectPath $LogFile
$resolvedLogDirectory = Split-Path -Path $resolvedLogFile -Parent

New-Item -ItemType Directory -Force -Path $resolvedLogDirectory | Out-Null

$arguments = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $resolvedProjectPath,
    "-executeMethod", "TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildWindowsContentUpdate",
    "-tinyHeroContentStatePath", $resolvedContentStatePath,
    "-logFile", $resolvedLogFile
)

$exitCode = Invoke-TinyHeroUnityProcess `
    -UnityExe $UnityExe `
    -ArgumentList $arguments `
    -LogFile $resolvedLogFile `
    -BuildLabel "Addressables Content Update"

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "[ Addressables Content Update Failure - Last 120 Lines ]"
    Get-Content -LiteralPath $resolvedLogFile -Tail 120 | ForEach-Object { Write-Host $_ }
    throw "TinyHero content update build failed. ExitCode: $exitCode. Log: $resolvedLogFile"
}

$publishScriptPath = Join-Path $PSScriptRoot "Publish-TinyHeroAddressablesContent.ps1"
& $publishScriptPath -SourcePath "ServerData" -PublishPath $PublishPath -LocalServerPath $LocalServerPath -ProjectPath $resolvedProjectPath

Write-Host "TinyHero content update completed. ContentStatePath: $resolvedContentStatePath"
