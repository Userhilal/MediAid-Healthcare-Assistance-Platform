# Script de démarrage pour MediAid
Write-Host "Démarrage de l'application MediAid..." -ForegroundColor Green
Write-Host ""

# Vérifier si MongoDB est accessible
Write-Host "Vérification de MongoDB..." -ForegroundColor Yellow
try {
    $mongoTest = Test-NetConnection -ComputerName localhost -Port 27017 -WarningAction SilentlyContinue
    if ($mongoTest.TcpTestSucceeded) {
        Write-Host "MongoDB est accessible sur le port 27017" -ForegroundColor Green
    } else {
        Write-Host "ATTENTION: MongoDB ne semble pas être accessible sur le port 27017" -ForegroundColor Red
        Write-Host "Assurez-vous que MongoDB est démarré avant de continuer." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Impossible de vérifier MongoDB: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Lancement de l'application..." -ForegroundColor Green
Write-Host "L'application sera accessible sur:" -ForegroundColor Cyan
Write-Host "  - HTTP:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "  - HTTPS: https://localhost:5001" -ForegroundColor Cyan
Write-Host ""
Write-Host "Appuyez sur Ctrl+C pour arrêter l'application" -ForegroundColor Yellow
Write-Host ""

# Lancer l'application
dotnet run

