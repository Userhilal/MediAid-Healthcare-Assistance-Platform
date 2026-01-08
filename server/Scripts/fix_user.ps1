# Script PowerShell pour corriger un utilisateur dans MongoDB
# Utilisez MongoDB .NET Driver si disponible, sinon utilisez mongosh

param(
    [Parameter(Mandatory=$true)]
    [string]$Email
)

Write-Host "Attempting to fix user: $Email" -ForegroundColor Cyan

# Option 1: If you have MongoDB Compass or mongosh installed
Write-Host "`nOption 1: Use MongoDB Compass or mongosh" -ForegroundColor Yellow
Write-Host "Run these commands in MongoDB Compass or mongosh:" -ForegroundColor Yellow
Write-Host @"
use Mediaid
db.Users.updateOne(
  { email: "$Email" },
  { 
    `$set: { 
      isEmailVerified: true,
      failedLoginAttempts: 0,
      lockoutEnd: null,
      isActive: true
    } 
  }
)
"@ -ForegroundColor Green

Write-Host "`nOption 2: Delete and recreate the user" -ForegroundColor Yellow
Write-Host "You can delete the user and create a new account with the same email" -ForegroundColor Yellow

