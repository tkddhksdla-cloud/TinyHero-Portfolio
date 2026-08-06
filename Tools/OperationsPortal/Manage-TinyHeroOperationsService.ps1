param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("JENKINS", "CONTENT", "PORTAL")]
    [string]$Service,

    [Parameter(Mandatory = $true)]
    [ValidateSet("START", "STOP")]
    [string]$Action
)

$ErrorActionPreference = "Stop"
$portalRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = (Resolve-Path (Join-Path $portalRoot "..\..")).Path
$tempRoot = Join-Path $projectRoot "Temp"
$servicePortMap = @{
    JENKINS = 8081
    CONTENT = 8082
    PORTAL = 8090
}
$servicePort = $servicePortMap[$Service]
$launcherPidPath = Join-Path $tempRoot "TinyHero$($Service)Launcher.pid"
$standardOutputPath = Join-Path $tempRoot "TinyHero$($Service).stdout.log"
$standardErrorPath = Join-Path $tempRoot "TinyHero$($Service).stderr.log"
$listenerArray = @(Get-NetTCPConnection -LocalPort $servicePort -State Listen -ErrorAction SilentlyContinue)

if ($Action -eq "STOP") {
    $allowedProcessNameArray = @("java", "dotnet", "TinyHero.OperationsPortal")

    foreach ($listener in $listenerArray) {
        $processId = [int]$listener.OwningProcess
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue

        if ($null -eq $process -or $process.ProcessName -notin $allowedProcessNameArray) {
            Write-Warning "Port $servicePort is used by an unexpected process. PID: $processId"
            continue
        }

        Stop-Process -Id $processId -Force
        Write-Host "Stopped $Service service. PID: $processId" -ForegroundColor Green
    }

    if (Test-Path -LiteralPath $launcherPidPath -PathType Leaf) {
        $launcherProcessIdText = Get-Content -LiteralPath $launcherPidPath -Raw
        $launcherProcessId = 0

        if ([int]::TryParse($launcherProcessIdText.Trim(), [ref]$launcherProcessId)) {
            $launcherProcess = Get-Process -Id $launcherProcessId -ErrorAction SilentlyContinue

            if ($null -ne $launcherProcess -and $launcherProcess.ProcessName -in @("powershell", "pwsh")) {
                Stop-Process -Id $launcherProcessId -Force
            }
        }

        Remove-Item -LiteralPath $launcherPidPath -Force -ErrorAction SilentlyContinue
    }

    return
}

if ($listenerArray.Count -gt 0) {
    Write-Host "$Service service is already running on port $servicePort." -ForegroundColor Green
    return
}

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
$powerShellExecutable = (Get-Process -Id $PID).Path

if ($Service -eq "JENKINS") {
    $startScriptPath = Join-Path $projectRoot "Tools\CI\Start-TinyHeroJenkins.ps1"
    $argumentArray = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $startScriptPath, "-NoBrowser")
}
else {
    $startScriptPath = Join-Path $portalRoot "Start-TinyHeroOperationsPortal.ps1"
    $argumentArray = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $startScriptPath, "-ServiceMode", $Service)
}

$serviceProcess = Start-Process `
    -FilePath $powerShellExecutable `
    -ArgumentList $argumentArray `
    -WindowStyle Hidden `
    -PassThru `
    -RedirectStandardOutput $standardOutputPath `
    -RedirectStandardError $standardErrorPath
Set-Content -LiteralPath $launcherPidPath -Value $serviceProcess.Id
Write-Host "Started $Service service launcher. PID: $($serviceProcess.Id)" -ForegroundColor Green
