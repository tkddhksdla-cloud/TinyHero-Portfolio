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

function Write-TinyHeroColoredLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value,
        [bool]$IsErrorLine = $false
    )

    if ($IsErrorLine -eq $false) {
        Write-Host $Value
        return
    }

    $escapeCharacter = [char]27
    $redText = "$escapeCharacter[31m$Value$escapeCharacter[0m"
    Write-Host $redText
}

function Test-TinyHeroErrorLogLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $result = $Value -match "error CS|Error|Exception|failed|Failed|Scripts have compiler errors|TinyHero Build|return code"
    return $result
}

function Write-TinyHeroBuildFailureSummary {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetLogFile
    )

    Write-Host ""
    Write-Host "========== TinyHero Build Failure Summary =========="

    if ((Test-Path -LiteralPath $TargetLogFile) -eq $false) {
        Write-Host "Build log was not found. Path: $TargetLogFile"
        Write-Host "===================================================="
        return
    }

    Write-Host "Log: $TargetLogFile"
    Write-Host ""
    Write-Host "[ Error Lines ]"

    $errorLines = Select-String -Path $TargetLogFile -Pattern "error CS|Error|Exception|failed|Failed|Scripts have compiler errors|TinyHero Build|return code" -CaseSensitive:$false | Select-Object -Last 80

    if ($null -eq $errorLines -or $errorLines.Count -eq 0) {
        Write-Host "No explicit error lines were found."
    }
    else {
        foreach ($errorLine in $errorLines) {
            Write-TinyHeroColoredLine -Value $errorLine.Line -IsErrorLine $true
        }
    }

    Write-Host ""
    Write-Host "[ Last 120 Log Lines ]"
    $tailLines = Get-Content -Path $TargetLogFile -Tail 120

    foreach ($tailLine in $tailLines) {
        $isErrorLine = Test-TinyHeroErrorLogLine -Value $tailLine
        Write-TinyHeroColoredLine -Value $tailLine -IsErrorLine $isErrorLine
    }

    Write-Host "===================================================="
    Write-Host ""
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
    Write-TinyHeroBuildFailureSummary -TargetLogFile $resolvedLogFile
    throw "TinyHero custom build failed. ExitCode: $exitCode. Log: $resolvedLogFile"
}

Write-Host "TinyHero custom build completed. Output: $BuildOutputPath"
