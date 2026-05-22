Write-Host "Stopping MediAid local processes..." -ForegroundColor Yellow

Get-Process MediAid -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "MediAid stopped." -ForegroundColor Green
