using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.Support;

namespace SprintTracker.Tests.Selenium.PageObjects;

/// <summary>
/// Page Object Model for Dashboard page
/// </summary>
public class DashboardPage : BasePage
{
    // Page URL
    private const string PageUrl = "/dashboard";

    // Locators
    private readonly By _welcomeMessage = By.CssSelector("h1, .welcome-message");
    private readonly By _projectsLink = By.CssSelector("a[href='/projects'], nav a:contains('Projects')");
    private readonly By _sprintsLink = By.CssSelector("a[href='/sprints'], nav a:contains('Sprints')");
    private readonly By _weatherLink = By.CssSelector("a[href='/weather'], nav a:contains('Weather')");
    private readonly By _logoutButton = By.CssSelector("button:contains('Logout'), [data-logout]");
    private readonly By _userMenu = By.CssSelector("[data-user-menu], .user-menu");
    
    // Dashboard stats
    private readonly By _totalProjects = By.CssSelector("[data-stat='projects'], .stat-projects");
    private readonly By _activeSprints = By.CssSelector("[data-stat='sprints'], .stat-sprints");
    private readonly By _totalTasks = By.CssSelector("[data-stat='tasks'], .stat-tasks");
    private readonly By _completionRate = By.CssSelector("[data-stat='completion'], .stat-completion");

    // Recent activity
    private readonly By _recentActivityList = By.CssSelector(".recent-activity, [data-recent-activity]");
    private readonly By _activityItems = By.CssSelector(".activity-item, [data-activity-item]");

    public DashboardPage(IWebDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Navigate to dashboard page
    /// </summary>
    public void Navigate()
    {
        NavigateTo($"{TestSettings.FrontendBaseUrl}{PageUrl}");
    }

    /// <summary>
    /// Check if on dashboard page
    /// </summary>
    public bool IsOnDashboard()
    {
        return GetCurrentUrl().Contains("/dashboard") && IsDisplayed(_welcomeMessage);
    }

    /// <summary>
    /// Get welcome message text
    /// </summary>
    public string GetWelcomeMessage()
    {
        return GetText(_welcomeMessage);
    }

    /// <summary>
    /// Navigate to Projects page
    /// </summary>
    public void NavigateToProjects()
    {
        Click(_projectsLink);
        WaitForUrlContains("/projects");
    }

    /// <summary>
    /// Navigate to Sprints page
    /// </summary>
    public void NavigateToSprints()
    {
        Click(_sprintsLink);
        WaitForUrlContains("/sprints");
    }

    /// <summary>
    /// Navigate to Weather page
    /// </summary>
    public void NavigateToWeather()
    {
        Click(_weatherLink);
        WaitForUrlContains("/weather");
    }

    /// <summary>
    /// Logout from application
    /// </summary>
    public void Logout()
    {
        // Try to click user menu first if it exists
        if (IsDisplayed(_userMenu))
        {
            Click(_userMenu);
            Thread.Sleep(500);
        }

        Click(_logoutButton);
        WaitForUrlContains("/login");
    }

    /// <summary>
    /// Get total projects count
    /// </summary>
    public string GetTotalProjects()
    {
        if (IsDisplayed(_totalProjects))
        {
            return GetText(_totalProjects);
        }
        return "0";
    }

    /// <summary>
    /// Get active sprints count
    /// </summary>
    public string GetActiveSprints()
    {
        if (IsDisplayed(_activeSprints))
        {
            return GetText(_activeSprints);
        }
        return "0";
    }

    /// <summary>
    /// Get total tasks count
    /// </summary>
    public string GetTotalTasks()
    {
        if (IsDisplayed(_totalTasks))
        {
            return GetText(_totalTasks);
        }
        return "0";
    }

    /// <summary>
    /// Get completion rate
    /// </summary>
    public string GetCompletionRate()
    {
        if (IsDisplayed(_completionRate))
        {
            return GetText(_completionRate);
        }
        return "0%";
    }

    /// <summary>
    /// Check if recent activity is displayed
    /// </summary>
    public bool IsRecentActivityDisplayed()
    {
        return IsDisplayed(_recentActivityList);
    }

    /// <summary>
    /// Get count of recent activity items
    /// </summary>
    public int GetRecentActivityCount()
    {
        if (IsDisplayed(_recentActivityList))
        {
            return FindElements(_activityItems).Count;
        }
        return 0;
    }
}
