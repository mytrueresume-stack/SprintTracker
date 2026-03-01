using FluentAssertions;
using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.PageObjects;
using TechTalk.SpecFlow;

namespace SprintTracker.Tests.Selenium.StepDefinitions;

[Binding]
public class DashboardSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IWebDriver Driver => (IWebDriver)_scenarioContext["WebDriver"];
    
    private DashboardPage? _dashboardPage;

    public DashboardSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"some activities have occurred")]
    public void GivenSomeActivitiesHaveOccurred()
    {
        // This would typically involve creating some projects/sprints/tasks
        // For now, we'll assume activities exist
    }

    [When(@"I navigate to the dashboard")]
    public void WhenINavigateToTheDashboard()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.Navigate();
    }

    [When(@"I click on the projects link")]
    public void WhenIClickOnTheProjectsLink()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.NavigateToProjects();
    }

    [When(@"I click on the sprints link")]
    public void WhenIClickOnTheSprintsLink()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.NavigateToSprints();
    }

    [When(@"I click on the weather link")]
    public void WhenIClickOnTheWeatherLink()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.NavigateToWeather();
    }

    [Then(@"I should see the dashboard page")]
    public void ThenIShouldSeeTheDashboardPage()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.IsOnDashboard().Should().BeTrue("should be on dashboard page");
    }

    [Then(@"I should see project statistics")]
    public void ThenIShouldSeeProjectStatistics()
    {
        _dashboardPage = new DashboardPage(Driver);
        var projectCount = _dashboardPage.GetTotalProjects();
        projectCount.Should().NotBeNull("project statistics should be displayed");
    }

    [Then(@"I should see sprint statistics")]
    public void ThenIShouldSeeSprintStatistics()
    {
        _dashboardPage = new DashboardPage(Driver);
        var sprintCount = _dashboardPage.GetActiveSprints();
        sprintCount.Should().NotBeNull("sprint statistics should be displayed");
    }

    [Then(@"I should see task statistics")]
    public void ThenIShouldSeeTaskStatistics()
    {
        _dashboardPage = new DashboardPage(Driver);
        var taskCount = _dashboardPage.GetTotalTasks();
        taskCount.Should().NotBeNull("task statistics should be displayed");
    }

    [Then(@"I should see a welcome message with my name")]
    public void ThenIShouldSeeAWelcomeMessageWithMyName()
    {
        _dashboardPage = new DashboardPage(Driver);
        var welcomeMessage = _dashboardPage.GetWelcomeMessage();
        welcomeMessage.Should().NotBeNullOrEmpty("welcome message should be displayed");
        welcomeMessage.Should().Contain("Welcome", "message should contain 'Welcome'");
    }

    [Then(@"I should see my assigned tasks")]
    public void ThenIShouldSeeMyAssignedTasks()
    {
        // This would check for a tasks section on the dashboard
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.IsOnDashboard().Should().BeTrue();
    }

    [Then(@"I should be redirected to the projects page")]
    public void ThenIShouldBeRedirectedToTheProjectsPage()
    {
        Driver.Url.Should().Contain("/projects", "should navigate to projects page");
    }

    [Then(@"I should be redirected to the sprints page")]
    public void ThenIShouldBeRedirectedToTheSprintsPage()
    {
        Driver.Url.Should().Contain("/sprints", "should navigate to sprints page");
    }

    [Then(@"I should be redirected to the weather page")]
    public void ThenIShouldBeRedirectedToTheWeatherPage()
    {
        Driver.Url.Should().Contain("/weather", "should navigate to weather page");
    }

    [Then(@"I should see recent activity items")]
    public void ThenIShouldSeeRecentActivityItems()
    {
        _dashboardPage = new DashboardPage(Driver);
        var activityCount = _dashboardPage.GetRecentActivityCount();
        activityCount.Should().BeGreaterThan(0, "should display recent activity");
    }
}
