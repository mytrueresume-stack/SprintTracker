# ============================================
# SprintTracker End-to-End Test Execution Script
# ============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SprintTracker E2E Test Suite Runner  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set error action preference
$ErrorActionPreference = "Continue"

# Configuration
$API_URL = "http://localhost:5000"
$FRONTEND_URL = "http://localhost:3000"
$MONGODB_PORT = 27017

# Test execution settings
$env:FRONTEND_URL = $FRONTEND_URL
$env:API_URL = $API_URL
$env:BROWSER = "chrome"
$env:HEADLESS = "false"
$env:DEFAULT_TIMEOUT = "15"
$env:SCREENSHOTS = "true"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  - API URL: $API_URL" -ForegroundColor Gray
Write-Host "  - Frontend URL: $FRONTEND_URL" -ForegroundColor Gray
Write-Host "  - Browser: chrome" -ForegroundColor Gray
Write-Host "  - Headless: false" -ForegroundColor Gray
Write-Host "  - Timeout: 15s" -ForegroundColor Gray
Write-Host ""

# Step 1: Check MongoDB
Write-Host "[1/5] Checking MongoDB..." -ForegroundColor Yellow
$mongoRunning = Test-NetConnection -ComputerName localhost -Port $MONGODB_PORT -InformationLevel Quiet -WarningAction SilentlyContinue
if ($mongoRunning) {
    Write-Host "  ? MongoDB is running on port $MONGODB_PORT" -ForegroundColor Green
} else {
    Write-Host "  ? MongoDB is NOT running!" -ForegroundColor Red
    Write-Host "  Please start MongoDB first:" -ForegroundColor Yellow
    Write-Host "  docker run -d -p 27017:27017 --name mongodb mongo:latest" -ForegroundColor Gray
    exit 1
}
Write-Host ""

# Step 2: Check API
Write-Host "[2/5] Checking API Backend..." -ForegroundColor Yellow
try {
    $apiResponse = Invoke-WebRequest -Uri "$API_URL/health" -Method GET -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    Write-Host "  ? API is running at $API_URL" -ForegroundColor Green
} catch {
    Write-Host "  ? API is NOT running!" -ForegroundColor Red
    Write-Host "  Please start the API in a separate terminal:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project SprintTracker.Api.csproj" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Press Enter to continue anyway or Ctrl+C to exit..." -ForegroundColor Yellow
    Read-Host
}
Write-Host ""

# Step 3: Check Frontend
Write-Host "[3/5] Checking Frontend UI..." -ForegroundColor Yellow
try {
    $frontendResponse = Invoke-WebRequest -Uri $FRONTEND_URL -Method GET -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    Write-Host "  ? Frontend is running at $FRONTEND_URL" -ForegroundColor Green
} catch {
    Write-Host "  ? Frontend is NOT running!" -ForegroundColor Red
    Write-Host "  Please start the frontend in a separate terminal:" -ForegroundColor Yellow
    Write-Host "  cd sprinttracker-ui" -ForegroundColor Gray
    Write-Host "  npm run dev" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Press Enter to continue anyway or Ctrl+C to exit..." -ForegroundColor Yellow
    Read-Host
}
Write-Host ""

# Step 4: Build Test Project
Write-Host "[4/5] Building Selenium Test Project..." -ForegroundColor Yellow
dotnet build SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ? Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ? Build successful" -ForegroundColor Green
Write-Host ""

# Step 5: Run Tests
Write-Host "[5/5] Running Selenium Tests..." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST EXECUTION STARTING" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Run all tests
dotnet test SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj --logger "console;verbosity=detailed"

# Check test results
if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ? ALL TESTS PASSED!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ? SOME TESTS FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check test results in:" -ForegroundColor Yellow
    Write-Host "  - SprintTracker.Tests.Selenium\TestResults\" -ForegroundColor Gray
    Write-Host "  - Screenshots for failures" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Test artifacts saved to: SprintTracker.Tests.Selenium\TestResults\" -ForegroundColor Cyan
Write-Host ""
