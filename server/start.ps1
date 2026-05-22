Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "   MediAid Development Startup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK is not installed or not available in PATH." -ForegroundColor Red
    exit 1
}

Write-Host "Checking .NET version..." -ForegroundColor Yellow
dotnet --version

Write-Host ""
Write-Host "Checking MongoDB on port 27017..." -ForegroundColor Yellow
$mongoTest = Test-NetConnection -ComputerName 127.0.0.1 -Port 27017 -WarningAction SilentlyContinue

if (-not $mongoTest.TcpTestSucceeded) {
    Write-Host "MongoDB is not running on port 27017." -ForegroundColor Red

    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "Docker detected. Starting MongoDB with docker compose..." -ForegroundColor Yellow
        docker compose up -d mongodb
        Start-Sleep -Seconds 5
    }
    else {
        Write-Host "Docker is not installed. Start MongoDB manually or install Docker Desktop." -ForegroundColor Red
        Write-Host "Then run this script again." -ForegroundColor Yellow
        exit 1
    }
}
else {
    Write-Host "MongoDB is running." -ForegroundColor Green
}

Write-Host ""
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host ""
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Fix the errors above before running." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting MediAid..." -ForegroundColor Green
Write-Host "HTTP:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "HTTPS: https://localhost:5001" -ForegroundColor Cyan
Write-Host "Health: http://localhost:5000/health" -ForegroundColor Cyan
Write-Host "Mongo Express: http://localhost:8081" -ForegroundColor Cyan
Write-Host ""

dotnet run --launch-profile http
