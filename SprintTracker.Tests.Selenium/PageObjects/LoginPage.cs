using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.Support;

namespace SprintTracker.Tests.Selenium.PageObjects;

/// <summary>
/// Page Object Model for Login page
/// </summary>
public class LoginPage : BasePage
{
    // Page URL
    private const string PageUrl = "/login";

    // Locators
    private readonly By _emailInput = By.Name("email");
    private readonly By _passwordInput = By.Name("password");
    private readonly By _loginButton = By.CssSelector("button[type='submit']");
    private readonly By _registerLink = By.LinkText("Register here");
    private readonly By _errorMessage = By.CssSelector("[role='alert'], .error-message, .text-red-500");
    private readonly By _loadingIndicator = By.CssSelector(".loading, [data-loading='true']");

    public LoginPage(IWebDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Navigate to login page
    /// </summary>
    public void Navigate()
    {
        NavigateTo($"{TestSettings.FrontendBaseUrl}{PageUrl}");
    }

    /// <summary>
    /// Enter email address
    /// </summary>
    public void EnterEmail(string email)
    {
        TypeText(_emailInput, email);
    }

    /// <summary>
    /// Enter password
    /// </summary>
    public void EnterPassword(string password)
    {
        TypeText(_passwordInput, password);
    }

    /// <summary>
    /// Click login button
    /// </summary>
    public void ClickLoginButton()
    {
        Click(_loginButton);
        
        // Wait for either navigation or error message
        try
        {
            WaitForUrlContains("/dashboard");
        }
        catch
        {
            // If navigation didn't happen, an error might be displayed
        }
    }

    /// <summary>
    /// Perform complete login action
    /// </summary>
    public void Login(string email, string password)
    {
        EnterEmail(email);
        EnterPassword(password);
        ClickLoginButton();
    }

    /// <summary>
    /// Click on register link
    /// </summary>
    public void ClickRegisterLink()
    {
        Click(_registerLink);
        WaitForUrlContains("/register");
    }

    /// <summary>
    /// Check if error message is displayed
    /// </summary>
    public bool IsErrorMessageDisplayed()
    {
        return IsDisplayed(_errorMessage);
    }

    /// <summary>
    /// Get error message text
    /// </summary>
    public string GetErrorMessage()
    {
        return GetText(_errorMessage);
    }

    /// <summary>
    /// Check if user is redirected to dashboard
    /// </summary>
    public bool IsRedirectedToDashboard()
    {
        return GetCurrentUrl().Contains("/dashboard");
    }

    /// <summary>
    /// Check if on login page
    /// </summary>
    public bool IsOnLoginPage()
    {
        return GetCurrentUrl().Contains("/login");
    }
}
