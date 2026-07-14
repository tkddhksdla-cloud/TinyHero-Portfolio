function ConvertTo-TinyHeroUnityProcessArgument {
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

function Write-TinyHeroUnityLogDelta {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LogFile,
        [Parameter(Mandatory = $true)]
        [ref]$LogPosition
    )

    if ((Test-Path -LiteralPath $LogFile -PathType Leaf) -eq $false) {
        return
    }

    try {
        $fileStream = [System.IO.File]::Open($LogFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)

        try {
            if ($fileStream.Length -lt $LogPosition.Value) {
                $LogPosition.Value = 0L
            }

            if ($fileStream.Length -eq $LogPosition.Value) {
                return
            }

            $null = $fileStream.Seek($LogPosition.Value, [System.IO.SeekOrigin]::Begin)
            $streamReader = [System.IO.StreamReader]::new($fileStream, [System.Text.Encoding]::UTF8, $true, 4096, $true)

            try {
                $newLogText = $streamReader.ReadToEnd()
                $LogPosition.Value = $fileStream.Length

                if ([string]::IsNullOrEmpty($newLogText) -eq $false) {
                    Write-Host -NoNewline $newLogText
                }
            }
            finally {
                $streamReader.Dispose()
            }
        }
        finally {
            $fileStream.Dispose()
        }
    }
    catch {
    }
}

function Invoke-TinyHeroUnityProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UnityExe,
        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList,
        [Parameter(Mandatory = $true)]
        [string]$LogFile,
        [Parameter(Mandatory = $true)]
        [string]$BuildLabel
    )

    if (Test-Path -LiteralPath $LogFile) {
        Clear-Content -LiteralPath $LogFile
    }

    $processStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processStartInfo.FileName = $UnityExe
    $processStartInfo.Arguments = ($ArgumentList | ForEach-Object { ConvertTo-TinyHeroUnityProcessArgument -Value $_ }) -join " "
    $processStartInfo.UseShellExecute = $false
    $processStartInfo.CreateNoWindow = $true

    Write-Host ""
    Write-Host "========== TinyHero Unity Process =========="
    Write-Host "Task: $BuildLabel"
    Write-Host "Unity: $UnityExe"
    Write-Host "Log: $LogFile"
    Write-Host "============================================"
    Write-Host ""

    $process = [System.Diagnostics.Process]::Start($processStartInfo)

    if ($null -eq $process) {
        throw "Unity process did not start. Path: $UnityExe"
    }

    Write-Host "[$BuildLabel] Unity process started. PID: $($process.Id)"
    $logPosition = 0L
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $nextHeartbeatSeconds = 15

    while ($process.WaitForExit(500) -eq $false) {
        Write-TinyHeroUnityLogDelta -LogFile $LogFile -LogPosition ([ref]$logPosition)

        if ($stopwatch.Elapsed.TotalSeconds -ge $nextHeartbeatSeconds) {
            $elapsedText = $stopwatch.Elapsed.ToString("hh\:mm\:ss")
            $logSize = if (Test-Path -LiteralPath $LogFile) { (Get-Item -LiteralPath $LogFile).Length } else { 0L }
            Write-Host "[$BuildLabel] Running... Elapsed: $elapsedText, LogBytes: $logSize"
            $nextHeartbeatSeconds += 15
        }
    }

    Write-TinyHeroUnityLogDelta -LogFile $LogFile -LogPosition ([ref]$logPosition)
    $stopwatch.Stop()
    $elapsedTimeText = $stopwatch.Elapsed.ToString("hh\:mm\:ss")
    Write-Host "[$BuildLabel] Unity process completed. ExitCode: $($process.ExitCode), Elapsed: $elapsedTimeText"
    [int]$result = $process.ExitCode
    return $result
}
