# SprintTracker Selenium Test Automation

## Overview
This is a comprehensive Selenium + BDD + C# test automation framework for SprintTracker application using:
- **SpecFlow** - BDD framework for writing tests in Gherkin syntax
- **NUnit** - Test execution framework
- **Selenium WebDriver** - Browser automation
- **Page Object Model** - Design pattern for maintainable tests
- **FluentAssertions** - Readable assertions

## Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 (recommended) or VS Code
- Chrome browser (or Firefox/Edge)
- SprintTracker API running on `http://localhost:5000`
- SprintTracker UI running on `http://localhost:3000`

## Project Structure
```
SprintTracker.Tests.Selenium/
??? Features/                    # BDD feature files (Gherkin scenarios)
?   ??? Authentication.feature
?   ??? ProjectManagement.feature
?   ??? SprintManagement.feature
?   ??? Dashboard.feature
??? StepDefinitions/            # Step implementations for scenarios
?   ??? AuthenticationSteps.cs
?   ??? ProjectSteps.cs
?   ??? SprintSteps.cs
?   ??? DashboardSteps.cs
??? PageObjects/                # Page Object Model classes
?   ??? BasePage.cs
?   ??? LoginPage.cs
?   ??? RegisterPage.cs
?   ??? DashboardPage.cs
?   ??? ProjectsPage.cs
?   ??? SprintsPage.cs
??? Drivers/                    # WebDriver factory and configuration
?   ??? WebDriverFactory.cs
??? Hooks/                      # SpecFlow hooks for test lifecycle
?   ??? TestHooks.cs
??? Support/                    # Test utilities and settings
    ??? TestSettings.cs
```

## Configuration
Tests can be configured using environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `FRONTEND_URL` | http://localhost:3000 | Next.js frontend URL |
| `API_URL` | http://localhost:5000 | ASP.NET Core API URL |
| `BROWSER` | chrome | Browser to use (chrome, firefox, edge) |
| `HEADLESS` | false | Run browser in headless mode |
| `DEFAULT_TIMEOUT` | 10 | Default wait timeout in seconds |
| `SCREENSHOTS` | true | Take screenshots on failure |

## Running Tests

### Visual Studio 2022
1. Open the solution in Visual Studio 2022
2. Build the solution
3. Open Test Explorer (Test > Test Explorer)
4. Run All Tests or select specific scenarios

### Command Line
```bash
# Run all tests
dotnet test

# Run specific feature
dotnet test --filter "Category=authentication"

# Run with specific browser
$env:BROWSER="chrome"; dotnet test

# Run in headless mode
$env:HEADLESS="true"; dotnet test

# Run smoke tests only
dotnet test --filter "Category=smoke"
```

### Using .runsettings
```bash
dotnet test --settings SprintTracker.Tests.Selenium/test.runsettings
```

## Test Categories
Tests are organized with the following tags:
- `@smoke` - Critical path tests
- `@authentication` - Login/logout/registration tests
- `@projects` - Project management tests
- `@sprints` - Sprint management tests
- `@dashboard` - Dashboard functionality tests
- `@negative` - Negative test scenarios
- `@authorization` - Role-based access tests

## Test Data
Default test users are configured in `TestSettings.cs`:

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@sprinttracker.com | Admin@123 |
| Manager | manager@sprinttracker.com | Manager@123 |
| Developer | developer@sprinttracker.com | Dev@123 |

## CI/CD Integration
The test suite is designed for CI/CD integration:

```yaml
# Example GitHub Actions
- name: Run Selenium Tests
  env:
    FRONTEND_URL: http://localhost:3000
    API_URL: http://localhost:5000
    BROWSER: chrome
    HEADLESS: true
  run: dotnet test --logger "trx;LogFileName=test-results.trx"
```

## Test Results
- Test results are saved to `TestResults/` directory
- Screenshots on failure are saved to `TestResults/`
- Logs are output to console and test results file

## Writing New Tests

### 1. Create Feature File
```gherkin
Feature: My New Feature
    As a user
    I want to do something
    So that I achieve a goal

Scenario: My test scenario
    Given I am on some page
    When I perform an action
    Then I should see expected result
```

### 2. Create Page Object (if needed)
```csharp
public class MyNewPage : BasePage
{
    private readonly By _element = By.Id("myElement");
    
    public MyNewPage(IWebDriver driver) : base(driver) { }
    
    public void DoSomething()
    {
        Click(_element);
    }
}
```

### 3. Create Step Definitions
```csharp
[Binding]
public class MyFeatureSteps
{
    [Given(@"I am on some page")]
    public void GivenIAmOnSomePage()
    {
        // Implementation
    }
}
```

## Troubleshooting

### ChromeDriver version mismatch
Update ChromeDriver package:
```bash
dotnet add package Selenium.WebDriver.ChromeDriver
```

### Tests failing with timeouts
Increase timeout in environment variables:
```bash
$env:DEFAULT_TIMEOUT="20"
```

### Element not found errors
- Check if selectors in Page Objects match UI elements
- Ensure proper waits are used
- Check if page is fully loaded before interaction

## Best Practices
1. **Use Page Object Model** - Keep page-specific logic in Page Objects
2. **Wait Explicitly** - Use explicit waits instead of Thread.Sleep
3. **Independent Tests** - Each scenario should be independent
4. **Meaningful Names** - Use descriptive scenario and step names
5. **Clean Test Data** - Use unique data for each test run
6. **Handle Waits** - Account for AJAX/async operations
7. **Tag Appropriately** - Use tags for filtering and organization

## Support
For issues or questions:
1. Check test results and screenshots in `TestResults/`
2. Review console output for detailed logs
3. Ensure application is running and accessible
4. Verify test data and configuration

## Next Steps (Playwright Migration)
This framework is designed to be easily converted to Playwright:
1. Replace Selenium packages with Playwright
2. Update WebDriverFactory to use Playwright browsers
3. Update Page Objects to use Playwright locators
4. Keep SpecFlow and step definitions mostly unchanged
