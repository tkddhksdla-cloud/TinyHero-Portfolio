param(
    [ValidateSet("PLAYER_BUILD", "CONTENT_UPDATE")]
    [string]$BuildMode = "PLAYER_BUILD",
    [string]$UnityExe = $env:UNITY_EXE,
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot/../..").Path,
    [string]$GameVersion = "0.0.01",
    [ValidateSet("ALL", "APK", "AAB")]
    [string]$ArtifactType = "ALL",
    [string]$BuildOutputPath = "Builds/Android/TinyHero.aab",
    [string]$ContentStatePath = "Assets/AddressableAssetsData/Android/addressables_content_state.bin",
    [string]$LogFile = "Builds/Android/Unity.log"
)

$ErrorActionPreference = "Stop"
$unityProcessScriptPath = Join-Path $PSScriptRoot "Invoke-TinyHeroUnityProcess.ps1"
. $unityProcessScriptPath

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe"
}

if ((Test-Path -LiteralPath $UnityExe) -eq $false) {
    throw "Unity executable not found. Set UNITY_EXE or pass -UnityExe. Path: $UnityExe"
}

$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedLogFile = Join-Path $resolvedProjectPath $LogFile
$resolvedLogDirectory = Split-Path -Path $resolvedLogFile -Parent

if ([string]::IsNullOrWhiteSpace($resolvedLogDirectory) -eq $false) {
    New-Item -ItemType Directory -Force -Path $resolvedLogDirectory | Out-Null
}

$argumentList = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $resolvedProjectPath
)

if ($BuildMode -eq "PLAYER_BUILD") {
    $argumentList += @(
        "-executeMethod", "TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildAndroidPlayer",
        "-tinyHeroGameVersion", $GameVersion,
        "-tinyHeroAndroidArtifactType", $ArtifactType,
        "-tinyHeroBuildOutputPath", $BuildOutputPath
    )
    $buildLabel = "Android Player Build"
}
else {
    $argumentList += @(
        "-executeMethod", "TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildAndroidContentUpdate",
        "-tinyHeroContentStatePath", $ContentStatePath
    )
    $buildLabel = "Android Content Update"
}

$argumentList += @("-logFile", $resolvedLogFile)
$exitCode = Invoke-TinyHeroUnityProcess `
    -UnityExe $UnityExe `
    -ArgumentList $argumentList `
    -LogFile $resolvedLogFile `
    -BuildLabel $buildLabel

if ($exitCode -ne 0) {
    Write-Host "[TinyHero Build] Android Unity log tail: $resolvedLogFile"

    if (Test-Path -LiteralPath $resolvedLogFile) {
        Get-Content -LiteralPath $resolvedLogFile -Tail 200
    }

    throw "$buildLabel failed. ExitCode: $exitCode. Log: $resolvedLogFile"
}

Write-Host "$buildLabel completed. Log: $resolvedLogFile"
