param(
    [Parameter(Mandatory = $true)]
    [string]$BuildPath,
    [Parameter(Mandatory = $true)]
    [string]$RemoteBaseUrl,
    [bool]$RequireRemoteContent = $true
)

$ErrorActionPreference = "Stop"

$resolvedBuildPath = (Resolve-Path -LiteralPath $BuildPath).Path
$dataDirectory = Get-ChildItem -LiteralPath $resolvedBuildPath -Directory -Filter "*_Data" | Select-Object -First 1

if ($null -eq $dataDirectory) {
    throw "Unity player data directory was not found under: $resolvedBuildPath"
}

$streamingAssetsPath = Join-Path $dataDirectory.FullName "StreamingAssets"
New-Item -ItemType Directory -Force -Path $streamingAssetsPath | Out-Null

$normalizedRemoteBaseUrl = $RemoteBaseUrl.Trim().TrimEnd('/', '\')
$config = [ordered]@{
    remoteBaseUrl = $normalizedRemoteBaseUrl
    requireRemoteContent = $RequireRemoteContent
}
$configPath = Join-Path $streamingAssetsPath "TinyHeroContentEndpoint.json"
$config | ConvertTo-Json | Set-Content -Path $configPath -Encoding UTF8

Write-Host "TinyHero content endpoint updated. Path: $configPath"
