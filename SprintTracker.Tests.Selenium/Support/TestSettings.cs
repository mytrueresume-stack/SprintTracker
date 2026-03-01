using System;

namespace SprintTracker.Tests.Selenium.Support;

/// <summary>
/// Centralized test configuration and settings
/// </summary>
public static class TestSettings
{
    /// <summary>
    /// Base URL for the Next.js frontend application
    /// </summary>
    public static string FrontendBaseUrl => Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";

    /// <summary>
    /// Base URL for the API backend
    /// </summary>
    public static string ApiBaseUrl => Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:5000";

    /// <summary>
    /// Default timeout for element waits (in seconds)
    /// </summary>
    public static int DefaultTimeout => int.TryParse(Environment.GetEnvironmentVariable("DEFAULT_TIMEOUT"), out var timeout) ? timeout : 10;

    /// <summary>
    /// Implicit wait time (in seconds)
    /// </summary>
    public static int ImplicitWait => int.TryParse(Environment.GetEnvironmentVariable("IMPLICIT_WAIT"), out var wait) ? wait : 5;

    /// <summary>
    /// Browser to use for testing (chrome, firefox, edge)
    /// </summary>
    public static string Browser => Environment.GetEnvironmentVariable("BROWSER") ?? "chrome";

    /// <summary>
    /// Run browser in headless mode
    /// </summary>
    public static bool Headless => bool.TryParse(Environment.GetEnvironmentVariable("HEADLESS"), out var headless) && headless;

    /// <summary>
    /// Take screenshots on test failure
    /// </summary>
    public static bool TakeScreenshotsOnFailure => !bool.TryParse(Environment.GetEnvironmentVariable("SCREENSHOTS"), out var screenshots) || screenshots;

    /// <summary>
    /// Directory for test artifacts (screenshots, logs)
    /// </summary>
    public static string ArtifactsDirectory => Environment.GetEnvironmentVariable("ARTIFACTS_DIR") ?? Path.Combine(Directory.GetCurrentDirectory(), "TestResults");

    /// <summary>
    /// Maximum time to wait for page load (in seconds)
    /// </summary>
    public static int PageLoadTimeout => int.TryParse(Environment.GetEnvironmentVariable("PAGE_LOAD_TIMEOUT"), out var timeout) ? timeout : 30;

    /// <summary>
    /// Test data for default users
    /// </summary>
    public static class TestUsers
    {
        public static class Admin
        {
            public static string Email => "venkateshboyapati96@gmail.com";
            public static string Password => "Govinda@1117";
            public static string FirstName => "Venkatesh";
            public static string LastName => "Boyapati";
        }

        public static class Manager
        {
            public static string Email => "venkateshboyapati96@gmail.com";
            public static string Password => "Govinda@1117";
            public static string FirstName => "Venkatesh";
            public static string LastName => "Boyapati";
        }

        public static class Developer
        {
            public static string Email => "venkateshboyapati96@gmail.com";
            public static string Password => "Govinda@1117";
            public static string FirstName => "Venkatesh";
            public static string LastName => "Boyapati";
        }
    }
}
