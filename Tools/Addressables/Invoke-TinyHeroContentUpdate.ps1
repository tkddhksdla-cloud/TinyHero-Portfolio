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

$processStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$processStartInfo.FileName = $UnityExe
$processStartInfo.Arguments = ($arguments | ForEach-Object { ConvertTo-UnityProcessArgument $_ }) -join " "
$processStartInfo.UseShellExecute = $false
$processStartInfo.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($processStartInfo)

if ($null -eq $process) {
    throw "Unity content update process did not start."
}

$process.WaitForExit()

if ($process.ExitCode -ne 0) {
    throw "TinyHero content update build failed. ExitCode: $($process.ExitCode). Log: $resolvedLogFile"
}

$publishScriptPath = Join-Path $PSScriptRoot "Publish-TinyHeroAddressablesContent.ps1"
& $publishScriptPath -SourcePath "ServerData" -PublishPath $PublishPath -LocalServerPath $LocalServerPath -ProjectPath $resolvedProjectPath

Write-Host "TinyHero content update completed. ContentStatePath: $resolvedContentStatePath"
