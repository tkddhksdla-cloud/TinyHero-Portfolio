param(
    [string]$PortalUrl = "http://127.0.0.1:8090",
    [string]$ContentUrl = "http://127.0.0.1:8082"
)

$ErrorActionPreference = "Stop"
$portalRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:ASPNETCORE_URLS = "$PortalUrl;$ContentUrl"

Write-Host "TinyHero Operations Portal: $PortalUrl"
Write-Host "TinyHero Content Server: $ContentUrl/TinyHeroContent"
dotnet run --project (Join-Path $portalRoot "TinyHero.OperationsPortal.csproj")
