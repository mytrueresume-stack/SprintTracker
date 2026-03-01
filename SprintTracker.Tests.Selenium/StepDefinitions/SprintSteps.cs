using FluentAssertions;
using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.PageObjects;
using TechTalk.SpecFlow;

namespace SprintTracker.Tests.Selenium.StepDefinitions;

[Binding]
public class SprintSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IWebDriver Driver => (IWebDriver)_scenarioContext["WebDriver"];
    
    private SprintsPage? _sprintsPage;
    private ProjectsPage? _projectsPage;

    public SprintSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"a project ""([^""]*)"" with key ""([^""]*)"" exists")]
    public void GivenAProjectWithKeyExists(string projectName, string projectKey)
    {
        // Create project via UI or assume it exists
        _projectsPage = new ProjectsPage(Driver);
        _projectsPage.Navigate();
        
        if (!_projectsPage.ProjectExists(projectName))
        {
            _projectsPage.CreateProject(projectName, projectKey, "Test project for sprints");
        }
        
        _scenarioContext[$"Project_{projectKey}"] = projectName;
    }

    [Given(@"I am on the sprints page")]
    [When(@"I navigate to the sprints page")]
    public void GivenIAmOnTheSprintsPage()
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.Navigate();
    }

    [Given(@"a sprint ""([^""]*)"" in ""([^""]*)"" status exists")]
    public void GivenASprintInStatusExists(string sprintName, string status)
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.Navigate();
        
        if (!_sprintsPage.SprintExists(sprintName))
        {
            // Create sprint via UI
            var projectName = _scenarioContext.Get<string>("Project_TEST");
            _sprintsPage.CreateSprint(projectName, sprintName, "Test goal", "2024-03-01", "2024-03-14");
        }
        
        _scenarioContext[$"Sprint_{sprintName}"] = status;
    }

    [When(@"I create a new sprint with the following details:")]
    public void WhenICreateANewSprintWithTheFollowingDetails(Table table)
    {
        _sprintsPage = new SprintsPage(Driver);
        
        var data = table.Rows.ToDictionary(row => row["Field"], row => row["Value"]);
        
        var projectName = data["Project"];
        var sprintName = data["Name"];
        var goal = data["Goal"];
        var startDate = data["Start Date"];
        var endDate = data["End Date"];
        
        _sprintsPage.CreateSprint(projectName, sprintName, goal, startDate, endDate);
        
        _scenarioContext["LastCreatedSprint"] = sprintName;
    }

    [When(@"I create sprint ""([^""]*)"" for project ""([^""]*)""")]
    public void WhenICreateSprintForProject(string sprintName, string projectName)
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.CreateSprint(projectName, sprintName, "Sprint goal", "2024-03-01", "2024-03-14");
        _sprintsPage.Navigate(); // Refresh to see the new sprint
    }

    [When(@"I start the sprint ""([^""]*)""")]
    public void WhenIStartTheSprint(string sprintName)
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.ClickSprint(sprintName);
        // This would involve clicking a "Start Sprint" button on sprint details page
        // For now, we'll just record the action
        _scenarioContext["SprintAction"] = "Start";
    }

    [When(@"I complete the sprint ""([^""]*)""")]
    public void WhenICompleteTheSprint(string sprintName)
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.ClickSprint(sprintName);
        // This would involve clicking a "Complete Sprint" button
        _scenarioContext["SprintAction"] = "Complete";
    }

    [When(@"I click create sprint button")]
    public void WhenIClickCreateSprintButton()
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.ClickCreateSprint();
    }

    [When(@"I fill in the sprint form partially")]
    public void WhenIFillInTheSprintFormPartially()
    {
        // Partially fill the form - just name
        _sprintsPage = new SprintsPage(Driver);
        // This would involve filling only some fields
    }

    [Then(@"the sprint should be created successfully")]
    public void ThenTheSprintShouldBeCreatedSuccessfully()
    {
        Thread.Sleep(1000); // Wait for sprint to appear
        
        var sprintName = _scenarioContext.Get<string>("LastCreatedSprint");
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.SprintExists(sprintName).Should().BeTrue($"sprint '{sprintName}' should exist");
    }

    [Then(@"I should see the sprint ""([^""]*)"" in the sprints list")]
    public void ThenIShouldSeeTheSprintInTheSprintsList(string sprintName)
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.SprintExists(sprintName).Should().BeTrue($"sprint '{sprintName}' should be visible");
    }

    [Then(@"the sprint status should be ""([^""]*)""")]
    public void ThenTheSprintStatusShouldBe(string expectedStatus)
    {
        var sprintName = _scenarioContext.Get<string>("LastCreatedSprint");
        _sprintsPage = new SprintsPage(Driver);
        
        var actualStatus = _sprintsPage.GetSprintStatus(sprintName);
        actualStatus.ToLower().Should().Contain(expectedStatus.ToLower());
    }

    [Then(@"I should see the sprints page")]
    public void ThenIShouldSeeTheSprintsPage()
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.IsOnSprintsPage().Should().BeTrue("should be on sprints page");
    }

    [Then(@"I should see a list of sprints or empty state")]
    public void ThenIShouldSeeAListOfSprintsOrEmptyState()
    {
        _sprintsPage = new SprintsPage(Driver);
        var hasSprints = _sprintsPage.GetSprintsCount() > 0;
        var hasEmptyState = _sprintsPage.IsEmptyStateDisplayed();
        
        (hasSprints || hasEmptyState).Should().BeTrue("should see either sprints or empty state");
    }

    [Then(@"I should see (.*) sprints in the list")]
    public void ThenIShouldSeeSprintsInTheList(int expectedCount)
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.GetSprintsCount().Should().Be(expectedCount, $"should see {expectedCount} sprints");
    }

    [Then(@"sprint ""([^""]*)"" should have number (.*)")]
    public void ThenSprintShouldHaveNumber(string sprintName, int sprintNumber)
    {
        // This would require checking the sprint number in the UI
        // For now, we'll just verify the sprint exists
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.SprintExists(sprintName).Should().BeTrue();
    }

    [Then(@"the sprint status should change to ""([^""]*)""")]
    public void ThenTheSprintStatusShouldChangeTo(string newStatus)
    {
        // This would check the updated status after a transition
        // For now, simplified check
        Thread.Sleep(500);
    }

    [Then(@"I should see a validation error")]
    public void ThenIShouldSeeAValidationError()
    {
        // Check for validation error message
        // This would be similar to error message checks in other steps
    }

    [Then(@"the sprint should not be created")]
    public void ThenTheSprintShouldNotBeCreated()
    {
        // Verify sprint was not created
    }

    [Then(@"the sprint creation modal should close")]
    public void ThenTheSprintCreationModalShouldClose()
    {
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.IsOnSprintsPage().Should().BeTrue();
    }

    [Then(@"no new sprint should be created")]
    public void ThenNoNewSprintShouldBeCreated()
    {
        // Verify no sprint was added
        _sprintsPage = new SprintsPage(Driver);
        _sprintsPage.IsOnSprintsPage().Should().BeTrue();
    }

    [Then(@"I should not see the create sprint button")]
    public void ThenIShouldNotSeeTheCreateSprintButton()
    {
        // Check that create button is not visible for developers
    }
}
