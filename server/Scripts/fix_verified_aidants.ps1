# Script pour réinitialiser le statut "Vérifié" des aidants
# Ce script met tous les aidants à IsVerified = false sauf ceux qui ont :
# - Au moins 5 missions complétées
# - Une note de réputation >= 4.0

Write-Host "Correction du statut 'Vérifié' des aidants..." -ForegroundColor Cyan

# Connexion à MongoDB
$connectionString = "mongodb://localhost:27017"
$databaseName = "MediAid"

try {
    # Charger le module MongoDB (nécessite MongoDB.Driver pour PowerShell)
    # Si le module n'est pas installé, vous devrez utiliser mongosh ou un autre outil
    
    Write-Host "NOTE: Ce script nécessite l'outil MongoDB Shell (mongosh) ou une connexion directe." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Pour exécuter cette correction manuellement avec mongosh, utilisez :" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "mongosh MediAid" -ForegroundColor Green
    Write-Host ""
    Write-Host "Puis exécutez :" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "db.aidants.updateMany(" -ForegroundColor Green
    Write-Host "  { `$or: [" -ForegroundColor Green
    Write-Host "    { completedMissions: { `$lt: 5 } }," -ForegroundColor Green
    Write-Host "    { reputationScore: { `$lt: 4.0 } }" -ForegroundColor Green
    Write-Host "  ] }," -ForegroundColor Green
    Write-Host "  { `$set: { isVerified: false } }" -ForegroundColor Green
    Write-Host ")" -ForegroundColor Green
    Write-Host ""
    Write-Host "Pour vérifier uniquement les aidants avec au moins 5 missions complétées et une note >= 4.0 :" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "db.aidants.updateMany(" -ForegroundColor Green
    Write-Host "  { `$and: [" -ForegroundColor Green
    Write-Host "    { completedMissions: { `$gte: 5 } }," -ForegroundColor Green
    Write-Host "    { reputationScore: { `$gte: 4.0 } }" -ForegroundColor Green
    Write-Host "  ] }," -ForegroundColor Green
    Write-Host "  { `$set: { isVerified: true } }" -ForegroundColor Green
    Write-Host ")" -ForegroundColor Green
    Write-Host ""
    Write-Host "OU pour réinitialiser TOUS les aidants à non vérifiés :" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "db.aidants.updateMany({}, { `$set: { isVerified: false } })" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host "Erreur: $_" -ForegroundColor Red
}


