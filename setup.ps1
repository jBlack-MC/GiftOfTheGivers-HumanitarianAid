#!/usr/bin/env pwsh
<#
  One-time setup script for Windows (PowerShell).
  Restores dependencies, restores local dotnet tools, and applies EF Core
  migrations against LocalDB.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

Write-Host "==> Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore GiftOfTheGivers.slnx

Write-Host "==> Restoring local dotnet tools (dotnet-ef)..." -ForegroundColor Cyan
dotnet tool restore

Write-Host "==> Applying EF Core migrations to LocalDB..." -ForegroundColor Cyan
dotnet ef database update `
  --project GiftOfTheGivers/GiftOfTheGivers.csproj `
  --startup-project GiftOfTheGivers/GiftOfTheGivers.csproj

Write-Host ""
Write-Host "Setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. (Optional) Add your own local secrets - see README.md 'Managing local secrets'."
Write-Host "  2. Run the app:"
Write-Host "       dotnet run --project GiftOfTheGivers/GiftOfTheGivers.csproj"
Write-Host "  3. Browse to the URL printed in the console (e.g. http://localhost:5106)."
Write-Host ""
Write-Host "Demo login (seeded automatically on first run):" -ForegroundColor Yellow
Write-Host "  Employee : employee@giftofthegivers.org / Employee#123"
Write-Host "  Donor    : donor@example.com / Donor#123"
