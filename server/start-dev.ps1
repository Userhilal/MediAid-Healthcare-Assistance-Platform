Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "        MediAid Development Run" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK is not installed." -ForegroundColor Red
    exit 1
}

Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
dotnet --version

Write-Host ""
Write-Host "Checking MongoDB on localhost:27017..." -ForegroundColor Yellow
$mongo = Test-NetConnection -ComputerName 127.0.0.1 -Port 27017 -WarningAction SilentlyContinue

if (-not $mongo.TcpTestSucceeded) {
    Write-Host "MongoDB is not running." -ForegroundColor Yellow

    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "Trying to start MongoDB with Docker Compose..." -ForegroundColor Yellow
        docker compose up -d mongodb
        Start-Sleep -Seconds 5
    }
    else {
        Write-Host "Docker is not available. Start MongoDB manually, then run this script again." -ForegroundColor Red
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
    Write-Host "Build failed. Fix the errors above first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting MediAid..." -ForegroundColor Green
Write-Host "App:      http://localhost:5000" -ForegroundColor Cyan
Write-Host "Mongo UI: http://localhost:8081" -ForegroundColor Cyan
Write-Host ""

dotnet run --launch-profile http
