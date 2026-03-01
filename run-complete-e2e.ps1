# ============================================
# Complete E2E Setup and Test Execution
# ============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SprintTracker Complete E2E Test Setup  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Kill any existing processes on ports 5000 and 3000
Write-Host "Cleaning up existing processes..." -ForegroundColor Yellow
Get-Process | Where-Object {$_.ProcessName -like "*node*" -or $_.ProcessName -like "*dotnet*"} | ForEach-Object {
    try {
        $ports = netstat -ano | Select-String ":(5000|3000)" | Select-String "LISTENING" | ForEach-Object {
            $_ -match "\s+(\d+)$" | Out-Null
            $matches[1]
        } | Select-Object -Unique
        
        foreach ($pid in $ports) {
            if ($_.Id -eq $pid) {
                Write-Host "  Stopping process $($_.ProcessName) (PID: $pid)" -ForegroundColor Gray
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            }
        }
    } catch {}
}
Start-Sleep -Seconds 2
Write-Host ""

# ============================================
# STEP 1: Start MongoDB
# ============================================
Write-Host "[1/4] Checking MongoDB..." -ForegroundColor Yellow
$mongoRunning = Test-NetConnection -ComputerName localhost -Port 27017 -InformationLevel Quiet -WarningAction SilentlyContinue
if ($mongoRunning) {
    Write-Host "  ? MongoDB is running on port 27017" -ForegroundColor Green
} else {
    Write-Host "  ? MongoDB is NOT running!" -ForegroundColor Red
    Write-Host "  Starting MongoDB with Docker..." -ForegroundColor Yellow
    docker run -d -p 27017:27017 --name mongodb mongo:latest 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? MongoDB started successfully" -ForegroundColor Green
        Start-Sleep -Seconds 5
    } else {
        Write-Host "  ? MongoDB container might already exist, trying to start it..." -ForegroundColor Gray
        docker start mongodb 2>$null
        Start-Sleep -Seconds 5
    }
}
Write-Host ""

# ============================================
# STEP 2: Start API Backend
# ============================================
Write-Host "[2/4] Starting API Backend..." -ForegroundColor Yellow
$apiRunning = Test-NetConnection -ComputerName localhost -Port 5000 -InformationLevel Quiet -WarningAction SilentlyContinue
if (-not $apiRunning) {
    Write-Host "  Starting SprintTracker API..." -ForegroundColor Gray
    
    # Start API in background job
    $apiJob = Start-Job -ScriptBlock {
        Set-Location $using:PWD
        dotnet run --project SprintTracker.Api.csproj
    }
    
    Write-Host "  Waiting for API to start..." -ForegroundColor Gray
    $maxWait = 30
    $waited = 0
    while ($waited -lt $maxWait) {
        Start-Sleep -Seconds 2
        $waited += 2
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 2 -UseBasicParsing -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "  ? API is running at http://localhost:5000" -ForegroundColor Green
                break
            }
        } catch {
            Write-Host "  ... waiting ($waited/$maxWait)" -ForegroundColor Gray
        }
    }
    
    if ($waited -ge $maxWait) {
        Write-Host "  ? API failed to start within $maxWait seconds" -ForegroundColor Red
        Write-Host "  Check the API output:" -ForegroundColor Yellow
        Receive-Job $apiJob
        exit 1
    }
} else {
    Write-Host "  ? API is already running at http://localhost:5000" -ForegroundColor Green
}
Write-Host ""

# ============================================
# STEP 3: Start Frontend UI
# ============================================
Write-Host "[3/4] Starting Frontend UI..." -ForegroundColor Yellow
$frontendRunning = Test-NetConnection -ComputerName localhost -Port 3000 -InformationLevel Quiet -WarningAction SilentlyContinue
if (-not $frontendRunning) {
    Write-Host "  Starting SprintTracker UI..." -ForegroundColor Gray
    
    # Start Frontend in background job
    $frontendJob = Start-Job -ScriptBlock {
        Set-Location "$using:PWD\sprinttracker-ui"
        npm run dev
    }
    
    Write-Host "  Waiting for Frontend to start..." -ForegroundColor Gray
    $maxWait = 60
    $waited = 0
    while ($waited -lt $maxWait) {
        Start-Sleep -Seconds 3
        $waited += 3
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:3000" -TimeoutSec 2 -UseBasicParsing -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "  ? Frontend is running at http://localhost:3000" -ForegroundColor Green
                break
            }
        } catch {
            Write-Host "  ... waiting ($waited/$maxWait)" -ForegroundColor Gray
        }
    }
    
    if ($waited -ge $maxWait) {
        Write-Host "  ? Frontend failed to start within $maxWait seconds" -ForegroundColor Red
        Write-Host "  Check the Frontend output:" -ForegroundColor Yellow
        Receive-Job $frontendJob
        exit 1
    }
} else {
    Write-Host "  ? Frontend is already running at http://localhost:3000" -ForegroundColor Green
}
Write-Host ""

# ============================================
# STEP 4: Run Selenium Tests
# ============================================
Write-Host "[4/4] Running Selenium Tests..." -ForegroundColor Yellow
Write-Host ""

# Set environment variables for tests
$env:FRONTEND_URL = "http://localhost:3000"
$env:API_URL = "http://localhost:5000"
$env:BROWSER = "chrome"
$env:HEADLESS = "false"
$env:DEFAULT_TIMEOUT = "20"
$env:SCREENSHOTS = "true"

Write-Host "Test Configuration:" -ForegroundColor Cyan
Write-Host "  - Login Email: venkateshboyapati96@gmail.com" -ForegroundColor Gray
Write-Host "  - Browser: Chrome (visible)" -ForegroundColor Gray
Write-Host "  - Timeout: 20 seconds" -ForegroundColor Gray
Write-Host ""

# Build test project
Write-Host "Building test project..." -ForegroundColor Yellow
dotnet build SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? Build successful" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RUNNING ALL 32 TEST SCENARIOS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Run all tests
dotnet test SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj --logger "console;verbosity=normal"

# Display results
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ??? ALL TESTS PASSED! ???" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Test Coverage Summary:" -ForegroundColor Green
    Write-Host "  ? Authentication: 9 tests" -ForegroundColor Green
    Write-Host "  ? Project Management: 7 tests" -ForegroundColor Green
    Write-Host "  ? Sprint Management: 8 tests" -ForegroundColor Green
    Write-Host "  ? Dashboard: 8 tests" -ForegroundColor Green
    Write-Host "  ? Total: 32 tests" -ForegroundColor Green
} else {
    Write-Host "  ? SOME TESTS FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check test results:" -ForegroundColor Yellow
    Write-Host "  - TestResults folder for screenshots" -ForegroundColor Gray
    Write-Host "  - Console output above for details" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Test artifacts location:" -ForegroundColor Cyan
Write-Host "  SprintTracker.Tests.Selenium\TestResults\" -ForegroundColor Gray
Write-Host ""

Write-Host "To stop services:" -ForegroundColor Yellow
Write-Host "  - Press Ctrl+C in API terminal" -ForegroundColor Gray
Write-Host "  - Press Ctrl+C in Frontend terminal" -ForegroundColor Gray
Write-Host "  - docker stop mongodb" -ForegroundColor Gray
Write-Host ""
