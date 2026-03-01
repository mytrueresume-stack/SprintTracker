# ============================================
# MANUAL E2E TEST EXECUTION GUIDE
# ============================================

## Prerequisites Check

### 1. MongoDB Status
Write-Host "Checking MongoDB..." -ForegroundColor Yellow
$mongoRunning = Test-NetConnection -ComputerName localhost -Port 27017 -InformationLevel Quiet -WarningAction SilentlyContinue
if ($mongoRunning) {
    Write-Host "? MongoDB is running" -ForegroundColor Green
} else {
    Write-Host "? MongoDB is NOT running" -ForegroundColor Red
    Write-Host "Start with: docker run -d -p 27017:27017 --name mongodb mongo:latest" -ForegroundColor Yellow
}

### 2. API Backend Status
Write-Host "Checking API Backend..." -ForegroundColor Yellow
$apiRunning = Test-NetConnection -ComputerName localhost -Port 5000 -InformationLevel Quiet -WarningAction SilentlyContinue
if ($apiRunning) {
    Write-Host "? API is running at http://localhost:5000" -ForegroundColor Green
} else {
    Write-Host "? API is NOT running" -ForegroundColor Red
    Write-Host "Start with: dotnet run --project SprintTracker.Api.csproj" -ForegroundColor Yellow
}

### 3. Frontend UI Status
Write-Host "Checking Frontend UI..." -ForegroundColor Yellow
$frontendRunning = Test-NetConnection -ComputerName localhost -Port 3000 -InformationLevel Quiet -WarningAction SilentlyContinue
if ($frontendRunning) {
    Write-Host "? Frontend is running at http://localhost:3000" -ForegroundColor Green
} else {
    Write-Host "? Frontend is NOT running" -ForegroundColor Red
    Write-Host "Start with:" -ForegroundColor Yellow
    Write-Host "  cd sprinttracker-ui" -ForegroundColor Yellow
    Write-Host "  npm run dev" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($mongoRunning -and $apiRunning -and $frontendRunning) {
    Write-Host "??? All services are running! Ready to test! ???" -ForegroundColor Green
    Write-Host ""
    Write-Host "Run tests with:" -ForegroundColor Cyan
    Write-Host "  .\run-tests.ps1" -ForegroundColor White
    Write-Host "  .\run-tests.ps1 -TestCategory smoke" -ForegroundColor White
    Write-Host "  .\run-tests.ps1 -TestCategory authentication" -ForegroundColor White
} else {
    Write-Host "Please start the missing services before running tests." -ForegroundColor Red
    Write-Host ""
    Write-Host "Quick Start Commands:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Terminal 1 - MongoDB:" -ForegroundColor Cyan
    Write-Host "  docker run -d -p 27017:27017 --name mongodb mongo:latest" -ForegroundColor White
    Write-Host ""
    Write-Host "Terminal 2 - API Backend:" -ForegroundColor Cyan
    Write-Host "  dotnet run --project SprintTracker.Api.csproj" -ForegroundColor White
    Write-Host ""
    Write-Host "Terminal 3 - Frontend UI:" -ForegroundColor Cyan
    Write-Host "  cd sprinttracker-ui" -ForegroundColor White
    Write-Host "  npm run dev" -ForegroundColor White
    Write-Host ""
    Write-Host "Terminal 4 - Run Tests:" -ForegroundColor Cyan
    Write-Host "  .\run-tests.ps1" -ForegroundColor White
}
