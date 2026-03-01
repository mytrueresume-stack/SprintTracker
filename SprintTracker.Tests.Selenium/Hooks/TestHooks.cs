using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.Drivers;
using SprintTracker.Tests.Selenium.Support;
using TechTalk.SpecFlow;

namespace SprintTracker.Tests.Selenium.Hooks;

/// <summary>
/// Hooks for SpecFlow test lifecycle management
/// </summary>
[Binding]
public class TestHooks
{
    private readonly ScenarioContext _scenarioContext;
    private IWebDriver? _driver;

    public TestHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Runs before each scenario - initializes WebDriver and sets up test artifacts directory
    /// </summary>
    [BeforeScenario]
    public void BeforeScenario()
    {
        // Ensure artifacts directory exists
        if (!Directory.Exists(TestSettings.ArtifactsDirectory))
        {
            Directory.CreateDirectory(TestSettings.ArtifactsDirectory);
        }

        // Create WebDriver instance
        _driver = WebDriverFactory.CreateWebDriver();
        _scenarioContext["WebDriver"] = _driver;

        // Log scenario start
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting scenario: {_scenarioContext.ScenarioInfo.Title}");
    }

    /// <summary>
    /// Runs after each scenario - handles cleanup and screenshots on failure
    /// </summary>
    [AfterScenario]
    public void AfterScenario()
    {
        try
        {
            // Take screenshot on failure
            if (_scenarioContext.TestError != null && TestSettings.TakeScreenshotsOnFailure && _driver != null)
            {
                TakeScreenshot("failure");
            }

            // Log scenario completion
            var status = _scenarioContext.TestError == null ? "PASSED" : "FAILED";
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Scenario {status}: {_scenarioContext.ScenarioInfo.Title}");

            if (_scenarioContext.TestError != null)
            {
                Console.WriteLine($"Error: {_scenarioContext.TestError.Message}");
            }
        }
        finally
        {
            // Cleanup WebDriver
            if (_driver != null)
            {
                try
                {
                    _driver.Quit();
                    _driver.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing WebDriver: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Takes a screenshot and saves it to the artifacts directory
    /// </summary>
    private void TakeScreenshot(string suffix)
    {
        try
        {
            if (_driver is ITakesScreenshot screenshotDriver)
            {
                var screenshot = screenshotDriver.GetScreenshot();
                var scenarioTitle = _scenarioContext.ScenarioInfo.Title
                    .Replace(" ", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_");
                
                var filename = $"{scenarioTitle}_{suffix}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filepath = Path.Combine(TestSettings.ArtifactsDirectory, filename);

                screenshot.SaveAsFile(filepath);
                Console.WriteLine($"Screenshot saved: {filepath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to take screenshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs before the entire test run
    /// </summary>
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Starting SprintTracker Selenium Test Suite");
        Console.WriteLine($"Frontend URL: {TestSettings.FrontendBaseUrl}");
        Console.WriteLine($"API URL: {TestSettings.ApiBaseUrl}");
        Console.WriteLine($"Browser: {TestSettings.Browser}");
        Console.WriteLine($"Headless: {TestSettings.Headless}");
        Console.WriteLine("=".PadRight(80, '='));
    }

    /// <summary>
    /// Runs after the entire test run
    /// </summary>
    [AfterTestRun]
    public static void AfterTestRun()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("SprintTracker Selenium Test Suite Completed");
        Console.WriteLine($"Artifacts saved to: {TestSettings.ArtifactsDirectory}");
        Console.WriteLine("=".PadRight(80, '='));
    }
}
