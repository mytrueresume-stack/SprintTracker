using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.Support;

namespace SprintTracker.Tests.Selenium.PageObjects;

/// <summary>
/// Page Object Model for Projects page
/// </summary>
public class ProjectsPage : BasePage
{
    // Page URL
    private const string PageUrl = "/projects";

    // Locators
    private readonly By _pageTitle = By.CssSelector("h1:contains('Projects'), .page-title");
    private readonly By _createProjectButton = By.CssSelector("button:contains('Create Project'), [data-create-project]");
    private readonly By _projectsList = By.CssSelector(".projects-list, [data-projects-list]");
    private readonly By _projectCards = By.CssSelector(".project-card, [data-project-card]");
    private readonly By _emptyState = By.CssSelector(".empty-state, [data-empty-projects]");
    
    // Create Project Modal
    private readonly By _projectNameInput = By.Name("name");
    private readonly By _projectKeyInput = By.Name("key");
    private readonly By _projectDescriptionInput = By.Name("description");
    private readonly By _startDateInput = By.Name("startDate");
    private readonly By _endDateInput = By.Name("targetEndDate");
    private readonly By _submitProjectButton = By.CssSelector("button[type='submit']:contains('Create'), [data-submit-project]");
    private readonly By _cancelButton = By.CssSelector("button:contains('Cancel'), [data-cancel]");
    private readonly By _modalOverlay = By.CssSelector(".modal, [role='dialog']");

    // Search and filters
    private readonly By _searchInput = By.CssSelector("input[placeholder*='Search'], [data-search]");
    private readonly By _statusFilter = By.CssSelector("select[name='status'], [data-filter-status]");

    public ProjectsPage(IWebDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Navigate to projects page
    /// </summary>
    public void Navigate()
    {
        NavigateTo($"{TestSettings.FrontendBaseUrl}{PageUrl}");
    }

    /// <summary>
    /// Check if on projects page
    /// </summary>
    public bool IsOnProjectsPage()
    {
        return GetCurrentUrl().Contains("/projects");
    }

    /// <summary>
    /// Click Create Project button
    /// </summary>
    public void ClickCreateProject()
    {
        Click(_createProjectButton);
        WaitForElementToBeVisible(_modalOverlay);
    }

    /// <summary>
    /// Fill in project creation form
    /// </summary>
    public void FillProjectForm(string name, string key, string description, string startDate = "", string endDate = "")
    {
        TypeText(_projectNameInput, name);
        TypeText(_projectKeyInput, key);
        TypeText(_projectDescriptionInput, description);
        
        if (!string.IsNullOrEmpty(startDate))
        {
            TypeText(_startDateInput, startDate);
        }
        
        if (!string.IsNullOrEmpty(endDate))
        {
            TypeText(_endDateInput, endDate);
        }
    }

    /// <summary>
    /// Submit project creation form
    /// </summary>
    public void SubmitProjectForm()
    {
        Click(_submitProjectButton);
        WaitForElementToDisappear(_modalOverlay);
    }

    /// <summary>
    /// Cancel project creation
    /// </summary>
    public void CancelProjectCreation()
    {
        Click(_cancelButton);
        WaitForElementToDisappear(_modalOverlay);
    }

    /// <summary>
    /// Create a new project (complete flow)
    /// </summary>
    public void CreateProject(string name, string key, string description)
    {
        ClickCreateProject();
        FillProjectForm(name, key, description);
        SubmitProjectForm();
    }

    /// <summary>
    /// Get count of projects displayed
    /// </summary>
    public int GetProjectsCount()
    {
        if (IsDisplayed(_projectsList))
        {
            return FindElements(_projectCards).Count;
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
    /// Search for projects
    /// </summary>
    public void SearchProjects(string searchTerm)
    {
        if (IsDisplayed(_searchInput))
        {
            TypeText(_searchInput, searchTerm);
            Thread.Sleep(500); // Wait for search to filter
        }
    }

    /// <summary>
    /// Click on a project by name
    /// </summary>
    public void ClickProject(string projectName)
    {
        var projectLocator = By.XPath($"//div[contains(@class, 'project-card') and contains(., '{projectName}')]");
        Click(projectLocator);
    }

    /// <summary>
    /// Check if project exists by name
    /// </summary>
    public bool ProjectExists(string projectName)
    {
        var projectLocator = By.XPath($"//div[contains(@class, 'project-card') and contains(., '{projectName}')]");
        return ElementExists(projectLocator);
    }

    /// <summary>
    /// Wait for modal to be visible
    /// </summary>
    private void WaitForElementToBeVisible(By locator)
    {
        FindVisibleElement(locator);
    }
}
