# ── DealTrack Publisher ────────────────────────────────────────────────────────
# Run this every time you want to push a new version to customers.
# Usage:  .\publish.ps1
# Usage:  .\publish.ps1 -Version "1.2"

param(
    [string]$Version = "latest"
)

$GITHUB_USER   = "abdulla662"
$REGISTRY      = "ghcr.io"
$BACKEND_IMAGE = "$REGISTRY/$GITHUB_USER/dealtrack-backend"
$FRONTEND_IMAGE= "$REGISTRY/$GITHUB_USER/dealtrack-frontend"
$FRONTEND_PATH = "C:\Users\USER\Desktop\MyProjectFrontEnd\FrontEndProjectFinal"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DealTrack Publisher v$Version" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ── Check token ───────────────────────────────────────────────────────────────
$tokenFile = "$PSScriptRoot\.github-token"
if (-not (Test-Path $tokenFile)) {
    Write-Host "ERROR: No GitHub token found." -ForegroundColor Red
    Write-Host "Run setup.ps1 first to save your token." -ForegroundColor Yellow
    exit 1
}
$token = Get-Content $tokenFile -Raw | ForEach-Object { $_.Trim() }

# ── Login to GitHub Container Registry ───────────────────────────────────────
Write-Host "[1/4] Logging into $REGISTRY..." -ForegroundColor Yellow
docker login $REGISTRY -u $GITHUB_USER -p $token 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Host "Login failed. Check your token." -ForegroundColor Red; exit 1 }
Write-Host "      Logged in OK" -ForegroundColor Green

# ── Build backend ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/4] Building backend..." -ForegroundColor Yellow
docker build -t "${BACKEND_IMAGE}:$Version" -t "${BACKEND_IMAGE}:latest" `
    -f DealTrack.API/Dockerfile .
if ($LASTEXITCODE -ne 0) { Write-Host "Backend build failed." -ForegroundColor Red; exit 1 }
Write-Host "      Backend built OK" -ForegroundColor Green

# ── Build frontend ────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[3/4] Building frontend..." -ForegroundColor Yellow
docker build -t "${FRONTEND_IMAGE}:$Version" -t "${FRONTEND_IMAGE}:latest" `
    --build-arg VITE_API_URL=/api `
    -f Dockerfile $FRONTEND_PATH
if ($LASTEXITCODE -ne 0) { Write-Host "Frontend build failed." -ForegroundColor Red; exit 1 }
Write-Host "      Frontend built OK" -ForegroundColor Green

# ── Push both images ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[4/4] Pushing to GitHub registry..." -ForegroundColor Yellow
docker push "${BACKEND_IMAGE}:$Version"
docker push "${BACKEND_IMAGE}:latest"
docker push "${FRONTEND_IMAGE}:$Version"
docker push "${FRONTEND_IMAGE}:latest"
if ($LASTEXITCODE -ne 0) { Write-Host "Push failed." -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  DONE! Version '$Version' is live." -ForegroundColor Green
Write-Host "  Send the customer-deploy\ folder to customers." -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
