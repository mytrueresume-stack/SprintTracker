using OpenQA.Selenium;
using SprintTracker.Tests.Selenium.Support;

namespace SprintTracker.Tests.Selenium.PageObjects;

/// <summary>
/// Page Object Model for Register page
/// </summary>
public class RegisterPage : BasePage
{
    // Page URL
    private const string PageUrl = "/register";

    // Locators
    private readonly By _firstNameInput = By.Name("firstName");
    private readonly By _lastNameInput = By.Name("lastName");
    private readonly By _emailInput = By.Name("email");
    private readonly By _passwordInput = By.Name("password");
    private readonly By _confirmPasswordInput = By.Name("confirmPassword");
    private readonly By _roleSelect = By.Name("role");
    private readonly By _registerButton = By.CssSelector("button[type='submit']");
    private readonly By _loginLink = By.LinkText("Login here");
    private readonly By _errorMessage = By.CssSelector("[role='alert'], .error-message, .text-red-500");
    private readonly By _successMessage = By.CssSelector(".success-message, .text-green-500");

    public RegisterPage(IWebDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Navigate to register page
    /// </summary>
    public void Navigate()
    {
        NavigateTo($"{TestSettings.FrontendBaseUrl}{PageUrl}");
    }

    /// <summary>
    /// Enter first name
    /// </summary>
    public void EnterFirstName(string firstName)
    {
        TypeText(_firstNameInput, firstName);
    }

    /// <summary>
    /// Enter last name
    /// </summary>
    public void EnterLastName(string lastName)
    {
        TypeText(_lastNameInput, lastName);
    }

    /// <summary>
    /// Enter email
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
    /// Enter confirm password
    /// </summary>
    public void EnterConfirmPassword(string confirmPassword)
    {
        TypeText(_confirmPasswordInput, confirmPassword);
    }

    /// <summary>
    /// Select role (Developer, Manager, Admin)
    /// </summary>
    public void SelectRole(string role)
    {
        SelectDropdownByText(_roleSelect, role);
    }

    /// <summary>
    /// Click register button
    /// </summary>
    public void ClickRegisterButton()
    {
        Click(_registerButton);
        
        // Wait for either success or error
        Thread.Sleep(1000);
    }

    /// <summary>
    /// Perform complete registration
    /// </summary>
    public void Register(string firstName, string lastName, string email, string password, string confirmPassword, string role = "Developer")
    {
        EnterFirstName(firstName);
        EnterLastName(lastName);
        EnterEmail(email);
        EnterPassword(password);
        EnterConfirmPassword(confirmPassword);
        SelectRole(role);
        ClickRegisterButton();
    }

    /// <summary>
    /// Click on login link
    /// </summary>
    public void ClickLoginLink()
    {
        Click(_loginLink);
        WaitForUrlContains("/login");
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
    /// Check if success message is displayed
    /// </summary>
    public bool IsSuccessMessageDisplayed()
    {
        return IsDisplayed(_successMessage);
    }

    /// <summary>
    /// Check if redirected to dashboard after successful registration
    /// </summary>
    public bool IsRedirectedToDashboard()
    {
        return GetCurrentUrl().Contains("/dashboard");
    }
}
