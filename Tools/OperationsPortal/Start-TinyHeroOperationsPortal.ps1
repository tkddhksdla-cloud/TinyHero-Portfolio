param(
    [string]$PortalUrl = "http://127.0.0.1:8090",
    [string]$ContentUrl = "http://127.0.0.1:8082",
    [ValidateSet("ALL", "PORTAL", "CONTENT")]
    [string]$ServiceMode = "ALL"
)

$ErrorActionPreference = "Stop"
$portalRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$serviceUrl = switch ($ServiceMode) {
    "PORTAL" { $PortalUrl }
    "CONTENT" { $ContentUrl }
    default { "$PortalUrl;$ContentUrl" }
}
$env:ASPNETCORE_URLS = $serviceUrl
$projectFilePath = Join-Path $portalRoot "TinyHero.OperationsPortal.csproj"
$runtimeDirectoryPath = Join-Path $portalRoot "bin\ServiceRuntime\$ServiceMode"
$portalAssemblyPath = Join-Path $runtimeDirectoryPath "TinyHero.OperationsPortal.dll"
Set-Location -LiteralPath $portalRoot

Write-Host "TinyHero Operations Service Mode: $ServiceMode"
Write-Host "Listening URL: $serviceUrl"
dotnet publish $projectFilePath --configuration Release --nologo --output $runtimeDirectoryPath
dotnet $portalAssemblyPath --urls $serviceUrl --ServiceMode=$ServiceMode
