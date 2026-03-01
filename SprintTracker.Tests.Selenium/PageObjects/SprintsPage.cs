using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.Support;

namespace SprintTracker.Tests.Selenium.PageObjects;

/// <summary>
/// Page Object Model for Sprints page
/// </summary>
public class SprintsPage : BasePage
{
    // Page URL
    private const string PageUrl = "/sprints";

    // Locators
    private readonly By _pageTitle = By.CssSelector("h1:contains('Sprints'), .page-title");
    private readonly By _createSprintButton = By.CssSelector("button:contains('Create Sprint'), [data-create-sprint]");
    private readonly By _sprintsList = By.CssSelector(".sprints-list, [data-sprints-list]");
    private readonly By _sprintCards = By.CssSelector(".sprint-card, [data-sprint-card]");
    private readonly By _emptyState = By.CssSelector(".empty-state, [data-empty-sprints]");
    
    // Create Sprint Modal
    private readonly By _projectSelect = By.Name("projectId");
    private readonly By _sprintNameInput = By.Name("name");
    private readonly By _sprintGoalInput = By.Name("goal");
    private readonly By _startDateInput = By.Name("startDate");
    private readonly By _endDateInput = By.Name("endDate");
    private readonly By _submitSprintButton = By.CssSelector("button[type='submit']:contains('Create')");
    private readonly By _cancelButton = By.CssSelector("button:contains('Cancel')");
    private readonly By _modalOverlay = By.CssSelector(".modal, [role='dialog']");

    // Sprint status badges
    private readonly By _planningBadge = By.CssSelector(".status-planning, [data-status='planning']");
    private readonly By _activeBadge = By.CssSelector(".status-active, [data-status='active']");
    private readonly By _completedBadge = By.CssSelector(".status-completed, [data-status='completed']");

    public SprintsPage(IWebDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Navigate to sprints page
    /// </summary>
    public void Navigate()
    {
        NavigateTo($"{TestSettings.FrontendBaseUrl}{PageUrl}");
    }

    /// <summary>
    /// Check if on sprints page
    /// </summary>
    public bool IsOnSprintsPage()
    {
        return GetCurrentUrl().Contains("/sprints");
    }

    /// <summary>
    /// Click Create Sprint button
    /// </summary>
    public void ClickCreateSprint()
    {
        Click(_createSprintButton);
        FindVisibleElement(_modalOverlay);
    }

    /// <summary>
    /// Fill in sprint creation form
    /// </summary>
    public void FillSprintForm(string projectName, string sprintName, string goal, string startDate, string endDate)
    {
        SelectDropdownByText(_projectSelect, projectName);
        TypeText(_sprintNameInput, sprintName);
        TypeText(_sprintGoalInput, goal);
        TypeText(_startDateInput, startDate);
        TypeText(_endDateInput, endDate);
    }

    /// <summary>
    /// Submit sprint creation form
    /// </summary>
    public void SubmitSprintForm()
    {
        Click(_submitSprintButton);
        WaitForElementToDisappear(_modalOverlay);
    }

    /// <summary>
    /// Create a new sprint (complete flow)
    /// </summary>
    public void CreateSprint(string projectName, string sprintName, string goal, string startDate, string endDate)
    {
        ClickCreateSprint();
        FillSprintForm(projectName, sprintName, goal, startDate, endDate);
        SubmitSprintForm();
    }

    /// <summary>
    /// Get count of sprints displayed
    /// </summary>
    public int GetSprintsCount()
    {
        if (IsDisplayed(_sprintsList))
        {
            return FindElements(_sprintCards).Count;
        }
        return 0;
    }

    /// <summary>
    /// Check if empty state is displayed
    /// </summary>
    public bool IsEmptyStateDisplayed()
    {
        return IsDisplayed(_emptyState);
    }

    /// <summary>
    /// Click on a sprint by name
    /// </summary>
    public void ClickSprint(string sprintName)
    {
        var sprintLocator = By.XPath($"//div[contains(@class, 'sprint-card') and contains(., '{sprintName}')]");
        Click(sprintLocator);
    }

    /// <summary>
    /// Check if sprint exists by name
    /// </summary>
    public bool SprintExists(string sprintName)
    {
        var sprintLocator = By.XPath($"//div[contains(@class, 'sprint-card') and contains(., '{sprintName}')]");
        return ElementExists(sprintLocator);
    }

    /// <summary>
    /// Get sprint status
    /// </summary>
    public string GetSprintStatus(string sprintName)
    {
        var statusLocator = By.XPath($"//div[contains(@class, 'sprint-card') and contains(., '{sprintName}')]//span[contains(@class, 'status')]");
        return GetText(statusLocator);
    }
}
