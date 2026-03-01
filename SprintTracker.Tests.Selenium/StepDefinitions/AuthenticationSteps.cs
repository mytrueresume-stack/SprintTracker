using FluentAssertions;
using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.PageObjects;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace SprintTracker.Tests.Selenium.StepDefinitions;

[Binding]
public class AuthenticationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IWebDriver Driver => (IWebDriver)_scenarioContext["WebDriver"];
    
    private LoginPage? _loginPage;
    private RegisterPage? _registerPage;
    private DashboardPage? _dashboardPage;

    public AuthenticationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"the application is running")]
    public void GivenTheApplicationIsRunning()
    {
        // This is a precondition check - could ping health endpoint if needed
        // For now, we assume the application is running
    }

    [Given(@"I am on the login page")]
    public void GivenIAmOnTheLoginPage()
    {
        _loginPage = new LoginPage(Driver);
        _loginPage.Navigate();
    }

    [Given(@"I am on the register page")]
    public void GivenIAmOnTheRegisterPage()
    {
        _registerPage = new RegisterPage(Driver);
        _registerPage.Navigate();
    }

    [Given(@"I am logged in as ""([^""]*)"" with password ""([^""]*)""")]
    public void GivenIAmLoggedInAsWithPassword(string email, string password)
    {
        _loginPage = new LoginPage(Driver);
        _loginPage.Navigate();
        _loginPage.Login(email, password);
        
        // Store login status in scenario context
        _scenarioContext["IsLoggedIn"] = true;
        _scenarioContext["LoggedInEmail"] = email;
    }

    [Given(@"I am not logged in")]
    public void GivenIAmNotLoggedIn()
    {
        // Ensure no session exists - could add logout logic here
        _scenarioContext["IsLoggedIn"] = false;
    }

    [Given(@"I am on the dashboard")]
    public void GivenIAmOnTheDashboard()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.Navigate();
    }

    [When(@"I login with email ""([^""]*)"" and password ""([^""]*)""")]
    public void WhenILoginWithEmailAndPassword(string email, string password)
    {
        _loginPage = new LoginPage(Driver);
        _loginPage.Login(email, password);
    }

    [When(@"I register with the following details:")]
    public void WhenIRegisterWithTheFollowingDetails(Table table)
    {
        _registerPage = new RegisterPage(Driver);
        
        var data = table.Rows.ToDictionary(row => row["Field"], row => row["Value"]);
        
        _registerPage.Register(
            data["First Name"],
            data["Last Name"],
            data["Email"],
            data["Password"],
            data["Confirm Password"],
            data.ContainsKey("Role") ? data["Role"] : "Developer"
        );
    }

    [When(@"I click on the register link")]
    public void WhenIClickOnTheRegisterLink()
    {
        _loginPage = new LoginPage(Driver);
        _loginPage.ClickRegisterLink();
    }

    [When(@"I click on the login link")]
    public void WhenIClickOnTheLoginLink()
    {
        _registerPage = new RegisterPage(Driver);
        _registerPage.ClickLoginLink();
    }

    [When(@"I click the logout button")]
    public void WhenIClickTheLogoutButton()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.Logout();
    }

    [Then(@"I should be redirected to the dashboard")]
    public void ThenIShouldBeRedirectedToTheDashboard()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.IsOnDashboard().Should().BeTrue("user should be on dashboard after successful login/registration");
    }

    [Then(@"I should be redirected to the login page")]
    public void ThenIShouldBeRedirectedToTheLoginPage()
    {
        _loginPage = new LoginPage(Driver);
        _loginPage.IsOnLoginPage().Should().BeTrue("user should be redirected to login page");
    }

    [Then(@"I should see a welcome message")]
    public void ThenIShouldSeeAWelcomeMessage()
    {
        _dashboardPage = new DashboardPage(Driver);
        var welcomeMessage = _dashboardPage.GetWelcomeMessage();
        welcomeMessage.Should().NotBeNullOrEmpty("welcome message should be displayed");
        welcomeMessage.Should().Contain("Welcome", "message should contain welcome text");
    }

    [Then(@"I should see an error message")]
    public void ThenIShouldSeeAnErrorMessage()
    {
        // Try both login and register pages
        if (_loginPage != null && _loginPage.IsOnLoginPage())
        {
            _loginPage.IsErrorMessageDisplayed().Should().BeTrue("error message should be displayed on login page");
        }
        else if (_registerPage != null)
        {
            _registerPage.IsErrorMessageDisplayed().Should().BeTrue("error message should be displayed on register page");
        }
    }

    [Then(@"I should remain on the login page")]
    public void ThenIShouldRemainOnTheLoginPage()
    {
        _loginPage = new LoginPage(Driver);
        _loginPage.IsOnLoginPage().Should().BeTrue("user should remain on login page after failed login");
    }

    [Then(@"I should remain on the register page")]
    public void ThenIShouldRemainOnTheRegisterPage()
    {
        _registerPage = new RegisterPage(Driver);
        Driver.Url.Should().Contain("/register", "user should remain on register page after failed registration");
    }

    [Then(@"I should be on the register page")]
    public void ThenIShouldBeOnTheRegisterPage()
    {
        Driver.Url.Should().Contain("/register", "user should be on register page");
    }

    [Then(@"I should be on the login page")]
    public void ThenIShouldBeOnTheLoginPage()
    {
        _loginPage = new LoginPage(Driver);
        _loginPage.IsOnLoginPage().Should().BeTrue("user should be on login page");
    }

    [When(@"I attempt to navigate to the dashboard")]
    public void WhenIAttemptToNavigateToTheDashboard()
    {
        _dashboardPage = new DashboardPage(Driver);
        _dashboardPage.Navigate();
    }
}
