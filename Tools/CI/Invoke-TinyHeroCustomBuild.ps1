param(
    [string]$UnityExe = $env:UNITY_EXE,
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot/../..").Path,
    [string]$BuildOutputPath = "Builds/Windows/TinyHero.exe",
    [string]$LogFile = "Logs/TinyHeroCustomBuild.log"
)

$ErrorActionPreference = "Stop"

function ConvertTo-UnityProcessArgument {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ($Value.Contains(" ") -eq $false) {
        return $Value
    }

    $escapedValue = $Value.Replace('"', '\"')
    return '"' + $escapedValue + '"'
}

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

$arguments = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $resolvedProjectPath,
    "-executeMethod", "TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildWindowsPlayer",
    "-tinyHeroBuildOutputPath", $BuildOutputPath,
    "-logFile", $resolvedLogFile
)

$processStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$processStartInfo.FileName = $UnityExe
$processStartInfo.Arguments = ($arguments | ForEach-Object { ConvertTo-UnityProcessArgument $_ }) -join " "
$processStartInfo.UseShellExecute = $false
$processStartInfo.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($processStartInfo)

if ($null -eq $process) {
    throw "Unity process did not start. Path: $UnityExe"
}

$process.WaitForExit()
$exitCode = $process.ExitCode

if ($exitCode -ne 0) {
    throw "TinyHero custom build failed. ExitCode: $exitCode. Log: $resolvedLogFile"
}

Write-Host "TinyHero custom build completed. Output: $BuildOutputPath"
