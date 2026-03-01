# ============================================
# SprintTracker Test Runner - Selective Tests
# ============================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("all", "smoke", "authentication", "projects", "sprints", "dashboard", "negative")]
    [string]$TestCategory = "all",
    
    [Parameter(Mandatory=$false)]
    [switch]$Headless = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SprintTracker Test Runner  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
$env:FRONTEND_URL = "http://localhost:3000"
$env:API_URL = "http://localhost:5000"
$env:BROWSER = "chrome"
$env:HEADLESS = if ($Headless) { "true" } else { "false" }
$env:DEFAULT_TIMEOUT = "15"
$env:SCREENSHOTS = "true"

# Build the test project
Write-Host "Building test project..." -ForegroundColor Yellow
dotnet build SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? Build successful" -ForegroundColor Green
Write-Host ""

# Determine filter
$filter = switch ($TestCategory) {
    "all" { "" }
    "smoke" { "--filter `"Category=smoke`"" }
    "authentication" { "--filter `"Category=authentication`"" }
    "projects" { "--filter `"Category=projects`"" }
    "sprints" { "--filter `"Category=sprints`"" }
    "dashboard" { "--filter `"Category=dashboard`"" }
    "negative" { "--filter `"Category=negative`"" }
    default { "" }
}

Write-Host "Running $TestCategory tests..." -ForegroundColor Yellow
Write-Host "Mode: $(if ($Headless) { 'Headless' } else { 'Visible Browser' })" -ForegroundColor Gray
Write-Host ""

# Run tests
$testCommand = "dotnet test SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj $filter --logger `"console;verbosity=normal`""
Invoke-Expression $testCommand

# Show results
Write-Host ""
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Tests Passed!" -ForegroundColor Green
} else {
    Write-Host "? Some tests failed. Check TestResults folder for details." -ForegroundColor Red
}

Write-Host ""
Write-Host "Test artifacts: SprintTracker.Tests.Selenium\TestResults\" -ForegroundColor Cyan
