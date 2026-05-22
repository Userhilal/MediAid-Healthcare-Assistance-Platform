param(
    [Parameter(Mandatory = $true)]
    [string]$Email,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Admin", "Expert")]
    [string]$Role
)

$ErrorActionPreference = "Stop"

$normalizedEmail = $Email.Trim().ToLowerInvariant()

Write-Host "MediAid Role Promotion Tool" -ForegroundColor Cyan
Write-Host "Email: $normalizedEmail"
Write-Host "Role:  $Role"

$mongoCheck = Test-NetConnection -ComputerName 127.0.0.1 -Port 27017 -WarningAction SilentlyContinue

if (-not $mongoCheck.TcpTestSucceeded) {
    Write-Host "MongoDB is not running on localhost:27017." -ForegroundColor Yellow

    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Push-Location "$PSScriptRoot\.."
        docker compose up -d mongodb
        Pop-Location
        Start-Sleep -Seconds 5
    }
    else {
        Write-Host "Start MongoDB manually, then run this script again." -ForegroundColor Red
        exit 1
    }
}

$escapedEmail = $normalizedEmail.Replace("'", "\'")
$escapedRole = $Role.Replace("'", "\'")

$js = @"
const email = '$escapedEmail';
const role = '$escapedRole';

const user = db.Users.findOne({ email: email });

if (!user) {
  print('ERROR: No user found with email: ' + email);
  quit(1);
}

db.Users.updateOne(
  { _id: user._id },
  {
    `$set: {
      role: role,
      isActive: true,
      updatedAt: new Date()
    }
  }
);

if (role === 'Expert') {
  db.Experts.updateOne(
    { userId: user._id },
    {
      `$setOnInsert: {
        userId: user._id,
        specialization: 'Healthcare request validation',
        organization: 'MediAid',
        validatedRequests: 0,
        createdAt: new Date(),
        updatedAt: new Date()
      }
    },
    { upsert: true }
  );
}

print('SUCCESS: ' + email + ' is now ' + role);
"@

$containerName = "mediaid-mongodb"

try {
    docker exec $containerName mongosh Mediaid --quiet --eval $js
}
catch {
    if (-not (Get-Command mongosh -ErrorAction SilentlyContinue)) {
        Write-Host "mongosh is not installed and Docker Mongo is not reachable." -ForegroundColor Red
        exit 1
    }

    mongosh "mongodb://localhost:27017/Mediaid" --quiet --eval $js
}

Write-Host "Done." -ForegroundColor Green
