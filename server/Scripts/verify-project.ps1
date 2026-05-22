$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "        MediAid Final Verification" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

$root = Resolve-Path "$PSScriptRoot\..\.."
$server = Join-Path $root "server"

$hasError = $false

function Fail($message) {
    Write-Host "[FAIL] $message" -ForegroundColor Red
    $script:hasError = $true
}

function Pass($message) {
    Write-Host "[OK] $message" -ForegroundColor Green
}

function Warn($message) {
    Write-Host "[WARN] $message" -ForegroundColor Yellow
}

Write-Host "Checking required files..." -ForegroundColor Cyan

$requiredFiles = @(
    "README.md",
    ".gitignore",
    ".github\workflows\dotnet-ci.yml",
    "server\MediAid.csproj",
    "server\Program.cs",
    "server\docker-compose.yml",
    "server\start-dev.ps1",
    "server\stop-dev.ps1",
    "server\Scripts\promote-role.ps1"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $root $file
    if (Test-Path $path) {
        Pass "$file exists"
    }
    else {
        Fail "$file is missing"
    }
}

Write-Host ""
Write-Host "Checking encoding issues..." -ForegroundColor Cyan

$codeFiles = Get-ChildItem $server -Recurse -File -Include *.cshtml,*.html,*.cs,*.js,*.css |
    Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }

$encodingIssues = Select-String -Path $codeFiles.FullName -Pattern "Ã|Â|â|�" -ErrorAction SilentlyContinue

if ($encodingIssues) {
    Fail "Encoding issues found:"
    $encodingIssues | Select-Object -First 20 | ForEach-Object {
        Write-Host "  $($_.Path):$($_.LineNumber) $($_.Line)" -ForegroundColor Red
    }
}
else {
    Pass "No broken encoding patterns found"
}

Write-Host ""
Write-Host "Checking backup files..." -ForegroundColor Cyan

$backupFiles = Get-ChildItem $server -Recurse -File |
    Where-Object { $_.Name -match "\.backup\.cs$|\.before.*\.cs$|_Layout\.backup\.cshtml$" }

if ($backupFiles) {
    Fail "Backup files found inside project:"
    $backupFiles | ForEach-Object {
        Write-Host "  $($_.FullName)" -ForegroundColor Red
    }
}
else {
    Pass "No backup source files found inside compiled project"
}

Write-Host ""
Write-Host "Checking README content..." -ForegroundColor Cyan

$readmePath = Join-Path $root "README.md"
if (Test-Path $readmePath) {
    $readme = Get-Content $readmePath -Raw

    if ($readme -match "MediAid Healthcare Assistance Platform") {
        Pass "README title is present"
    }
    else {
        Fail "README title is missing"
    }

    if ($readme -match "Cookie Authentication") {
        Pass "README explains Cookie Authentication"
    }
    else {
        Warn "README does not clearly mention Cookie Authentication"
    }

    if ($readme -match "Creating Admin and Expert Accounts") {
        Pass "README explains Admin/Expert account creation"
    }
    else {
        Warn "README does not explain Admin/Expert account creation"
    }
}

Write-Host ""
Write-Host "Checking Git tracked uploads..." -ForegroundColor Cyan

Push-Location $root
$trackedUploads = git ls-files "server/wwwroot/uploads/*"

if ($trackedUploads) {
    $badUploads = $trackedUploads | Where-Object { $_ -notmatch "\.gitkeep$" }

    if ($badUploads) {
        Fail "Runtime upload files are tracked by Git:"
        $badUploads | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    }
    else {
        Pass "Only upload .gitkeep files are tracked"
    }
}
else {
    Pass "No runtime uploads are tracked"
}
Pop-Location

Write-Host ""
Write-Host "Running .NET build..." -ForegroundColor Cyan

Push-Location $server
dotnet clean
dotnet restore
dotnet build

if ($LASTEXITCODE -eq 0) {
    Pass ".NET build succeeded"
}
else {
    Fail ".NET build failed"
}
Pop-Location

Write-Host ""
Write-Host "Checking Git status..." -ForegroundColor Cyan

Push-Location $root
git status --short

if ($LASTEXITCODE -eq 0) {
    Pass "Git status command completed"
}
else {
    Warn "Could not read git status"
}
Pop-Location

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan

if ($hasError) {
    Write-Host "Verification finished with errors." -ForegroundColor Red
    exit 1
}

Write-Host "Verification passed. Project looks ready." -ForegroundColor Green
exit 0
