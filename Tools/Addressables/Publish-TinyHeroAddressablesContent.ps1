param(
    [string]$SourcePath = "ServerData",
    [string]$PublishPath = "PublishedContent",
    [string]$LocalServerPath = "",
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot/../..").Path
)

$ErrorActionPreference = "Stop"

function Resolve-TinyHeroPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedSourcePath = Resolve-TinyHeroPath -Path $SourcePath -BasePath $resolvedProjectPath

if ((Test-Path -LiteralPath $resolvedSourcePath -PathType Container) -eq $false) {
    throw "Addressables content source was not found. Path: $resolvedSourcePath"
}

$resolvedPublishPath = Resolve-TinyHeroPath -Path $PublishPath -BasePath $resolvedProjectPath
New-Item -ItemType Directory -Force -Path $resolvedPublishPath | Out-Null
Copy-Item -Path (Join-Path $resolvedSourcePath "*") -Destination $resolvedPublishPath -Recurse -Force

$resolvedLocalServerPath = ""

if ([string]::IsNullOrWhiteSpace($LocalServerPath) -eq $false) {
    $resolvedLocalServerPath = Resolve-TinyHeroPath -Path $LocalServerPath -BasePath $resolvedProjectPath
    New-Item -ItemType Directory -Force -Path $resolvedLocalServerPath | Out-Null
    Copy-Item -Path (Join-Path $resolvedSourcePath "*") -Destination $resolvedLocalServerPath -Recurse -Force
}

$manifest = [ordered]@{
    publishedAtUtc = [DateTime]::UtcNow.ToString("o")
    sourcePath = $resolvedSourcePath
    publishPath = $resolvedPublishPath
    localServerPath = $resolvedLocalServerPath
}
$manifestPath = Join-Path $resolvedPublishPath "TinyHeroContentManifest.json"
$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "TinyHero Addressables content published. PublishPath: $resolvedPublishPath"

if ([string]::IsNullOrWhiteSpace($resolvedLocalServerPath) -eq $false) {
    Write-Host "TinyHero local content server synchronized. LocalServerPath: $resolvedLocalServerPath"
}
