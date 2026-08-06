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

Write-Host "TinyHero Operations Service Mode: $ServiceMode"
Write-Host "Listening URL: $serviceUrl"
dotnet run --project (Join-Path $portalRoot "TinyHero.OperationsPortal.csproj") -- --ServiceMode=$ServiceMode
