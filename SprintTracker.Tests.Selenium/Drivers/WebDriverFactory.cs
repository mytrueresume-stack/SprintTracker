using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using SprintTracker.Tests.Selenium.Support;
using System;

namespace SprintTracker.Tests.Selenium.Drivers;

/// <summary>
/// WebDriver factory for creating and configuring browser instances
/// </summary>
public class WebDriverFactory
{
    /// <summary>
    /// Creates a configured WebDriver instance based on test settings
    /// </summary>
    public static IWebDriver CreateWebDriver()
    {
        var browser = TestSettings.Browser.ToLower();
        
        IWebDriver driver = browser switch
        {
            "chrome" => CreateChromeDriver(),
            "firefox" => CreateFirefoxDriver(),
            "edge" => CreateEdgeDriver(),
            _ => throw new ArgumentException($"Unsupported browser: {browser}")
        };

        // Configure timeouts
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestSettings.ImplicitWait);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(TestSettings.PageLoadTimeout);

        // Maximize window
        driver.Manage().Window.Maximize();

        return driver;
    }

    private static IWebDriver CreateChromeDriver()
    {
        var options = new ChromeOptions();
        
        if (TestSettings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        // Common Chrome arguments for stability
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);

        // Suppress console logs
        options.AddArgument("--log-level=3");
        options.AddArgument("--silent");

        return new ChromeDriver(options);
    }

    private static IWebDriver CreateFirefoxDriver()
    {
        var options = new FirefoxOptions();
        
        if (TestSettings.Headless)
        {
            options.AddArgument("--headless");
        }

        options.AddArgument("--width=1920");
        options.AddArgument("--height=1080");

        return new FirefoxDriver(options);
    }

    private static IWebDriver CreateEdgeDriver()
    {
        var options = new EdgeOptions();
        
        if (TestSettings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        return new EdgeDriver(options);
    }
}
