# ?? Running End-to-End Selenium Tests

## ? What Was Fixed

### 1. **React Hooks Error Fixed**
   - Moved `useState` hooks to the top of `SprintReportPage` component
   - All hooks now called before conditional returns (Rules of Hooks compliant)

### 2. **Test Credentials Updated**
   - All tests now use: **venkateshboyapati96@gmail.com** / **Govinda@1117**
   - Updated in: TestSettings.cs, Authentication.feature, ProjectManagement.feature, SprintManagement.feature, Dashboard.feature

### 3. **Build Errors Fixed**
   - Test project builds successfully
   - All dependencies are correctly referenced

## ?? Prerequisites (MUST BE RUNNING)

You need **3 services running** in separate terminals:

### Terminal 1: MongoDB
```powershell
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

### Terminal 2: API Backend
```powershell
dotnet run --project SprintTracker.Api.csproj
```
**Expected output:** `Now listening on: http://localhost:5000`

### Terminal 3: Frontend UI
```powershell
cd sprinttracker-ui
npm run dev
```
**Expected output:** `Ready on http://localhost:3000`

---

## ?? Quick Start - Run Tests

### Option 1: Check Services First
```powershell
.\check-services.ps1
```
This will tell you which services are running and which need to be started.

### Option 2: Run All Tests (32 scenarios)
```powershell
.\run-tests.ps1
```

### Option 3: Run Specific Test Categories
```powershell
# Run only smoke tests (quick validation)
.\run-tests.ps1 -TestCategory smoke

# Run authentication tests only (9 tests)
.\run-tests.ps1 -TestCategory authentication

# Run project management tests (7 tests)
.\run-tests.ps1 -TestCategory projects

# Run sprint management tests (8 tests)
.\run-tests.ps1 -TestCategory sprints

# Run dashboard tests (8 tests)
.\run-tests.ps1 -TestCategory dashboard

# Run negative test scenarios
.\run-tests.ps1 -TestCategory negative

# Run in headless mode (faster)
.\run-tests.ps1 -TestCategory smoke -Headless
```

---

## ?? Test Scenarios Overview

### ? Authentication Tests (9 scenarios)
- ? User registration with Developer role
- ? User registration with Manager role
- ? Successful user login with **venkateshboyapati96@gmail.com**
- ? Login with invalid credentials (negative)
- ? Registration with mismatched passwords (negative)
- ? Registration with existing email (negative)
- ? Navigation between login/register pages
- ? Successful logout

### ? Project Management Tests (7 scenarios)
- ? Create new project
- ? View projects list
- ? Create project with dates
- ? Duplicate project key validation (negative)
- ? Search for projects
- ? Cancel project creation
- ? Developer authorization check

### ? Sprint Management Tests (8 scenarios)
- ? Create new sprint
- ? View sprints list
- ? Multiple sprints for same project
- ? Sprint lifecycle: Planning ? Active
- ? Sprint lifecycle: Active ? Completed
- ? Invalid date validation (negative)
- ? Cancel sprint creation
- ? Developer authorization check

### ? Dashboard Tests (8 scenarios)
- ? Admin dashboard with statistics
- ? Manager dashboard view
- ? Developer dashboard view
- ? Navigate to projects
- ? Navigate to sprints
- ? Navigate to weather
- ? Recent activity display
- ? Unauthenticated access prevention

---

## ?? Manual Test Execution

If you prefer to run tests manually:

```powershell
# Build the test project
dotnet build SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj

# Run all tests
dotnet test SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~SuccessfulUserLogin"

# Run with detailed output
dotnet test SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj --logger "console;verbosity=detailed"
```

---

## ?? Troubleshooting

### Problem: "ERR_CONNECTION_REFUSED"
**Solution:** Frontend or API is not running. Check with `.\check-services.ps1`

### Problem: "Element not found" or timeouts
**Solution:** Increase timeout:
```powershell
$env:DEFAULT_TIMEOUT = "30"
.\run-tests.ps1
```

### Problem: Chrome driver version mismatch
**Solution:** Update ChromeDriver:
```powershell
dotnet add SprintTracker.Tests.Selenium\SprintTracker.Tests.Selenium.csproj package Selenium.WebDriver.ChromeDriver
```

### Problem: Tests fail to login
**Solution:** Verify the user exists in database:
- Email: venkateshboyapati96@gmail.com
- Password: Govinda@1117
- The user should be registered before running tests

### Problem: React Hooks error in UI
**Solution:** Already fixed! The `useState` hooks are now at the top of the component.

---

## ?? Test Artifacts

After test execution, check:
```
SprintTracker.Tests.Selenium\TestResults\
  ??? test-results.trx          # Test results file
  ??? *_failure_*.png           # Screenshots of failed tests
  ??? logs/                     # Test execution logs
```

---

## ?? Complete E2E Execution Example

```powershell
# Step 1: Check all services
.\check-services.ps1

# Step 2: If all running, execute tests
.\run-tests.ps1

# Step 3: View results
# - Console output shows pass/fail
# - TestResults folder has screenshots
```

---

## ?? Tips

1. **First Time Running?** 
   - Ensure user venkateshboyapati96@gmail.com is registered in the app
   - Run smoke tests first: `.\run-tests.ps1 -TestCategory smoke`

2. **Debugging Tests?**
   - Set `$env:HEADLESS = "false"` to see browser
   - Set `$env:DEFAULT_TIMEOUT = "30"` for slower systems

3. **CI/CD Pipeline?**
   - Use headless mode: `.\run-tests.ps1 -Headless`
   - Check `.github\workflows\selenium-tests.yml`

---

## ?? Expected Results

? **All 32 tests should pass** if:
- MongoDB is running
- API is running at http://localhost:5000
- Frontend is running at http://localhost:3000
- User venkateshboyapati96@gmail.com exists with password Govinda@1117

---

## ?? Need Help?

Run: `.\check-services.ps1` to diagnose which services are not running.
