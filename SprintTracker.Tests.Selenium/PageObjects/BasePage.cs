using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SprintTracker.Tests.Selenium.Support;

namespace SprintTracker.Tests.Selenium.PageObjects;

/// <summary>
/// Base page object class with common functionality for all pages
/// </summary>
public abstract class BasePage
{
    protected readonly IWebDriver Driver;
    protected readonly WebDriverWait Wait;

    protected BasePage(IWebDriver driver)
    {
        Driver = driver;
        Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(TestSettings.DefaultTimeout));
    }

    /// <summary>
    /// Navigate to a specific URL
    /// </summary>
    protected void NavigateTo(string url)
    {
        Driver.Navigate().GoToUrl(url);
        WaitForPageLoad();
    }

    /// <summary>
    /// Wait for page to be fully loaded
    /// </summary>
    protected void WaitForPageLoad()
    {
        Wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState")?.Equals("complete") ?? false);
    }

    /// <summary>
    /// Find element with explicit wait
    /// </summary>
    protected IWebElement FindElement(By locator)
    {
        return Wait.Until(ExpectedConditions.ElementExists(locator));
    }

    /// <summary>
    /// Find visible element with explicit wait
    /// </summary>
    protected IWebElement FindVisibleElement(By locator)
    {
        return Wait.Until(ExpectedConditions.ElementIsVisible(locator));
    }

    /// <summary>
    /// Find clickable element with explicit wait
    /// </summary>
    protected IWebElement FindClickableElement(By locator)
    {
        return Wait.Until(ExpectedConditions.ElementToBeClickable(locator));
    }

    /// <summary>
    /// Find all elements matching the locator
    /// </summary>
    protected IReadOnlyCollection<IWebElement> FindElements(By locator)
    {
        Wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(locator));
        return Driver.FindElements(locator);
    }

    /// <summary>
    /// Type text into an input field
    /// </summary>
    protected void TypeText(By locator, string text)
    {
        var element = FindVisibleElement(locator);
        element.Clear();
        element.SendKeys(text);
    }

    /// <summary>
    /// Click an element
    /// </summary>
    protected void Click(By locator)
    {
        FindClickableElement(locator).Click();
    }

    /// <summary>
    /// Get text from an element
    /// </summary>
    protected string GetText(By locator)
    {
        return FindVisibleElement(locator).Text;
    }

    /// <summary>
    /// Check if element exists on the page
    /// </summary>
    protected bool ElementExists(By locator)
    {
        try
        {
            Driver.FindElement(locator);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    /// <summary>
    /// Check if element is displayed
    /// </summary>
    protected bool IsDisplayed(By locator)
    {
        try
        {
            return Driver.FindElement(locator).Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for element to disappear
    /// </summary>
    protected void WaitForElementToDisappear(By locator)
    {
        Wait.Until(ExpectedConditions.InvisibilityOfElementLocated(locator));
    }

    /// <summary>
    /// Wait for specific text to appear in element
    /// </summary>
    protected void WaitForTextInElement(By locator, string text)
    {
        Wait.Until(ExpectedConditions.TextToBePresentInElementLocated(locator, text));
    }

    /// <summary>
    /// Execute JavaScript
    /// </summary>
    protected object? ExecuteScript(string script, params object[] args)
    {
        return ((IJavaScriptExecutor)Driver).ExecuteScript(script, args);
    }

    /// <summary>
    /// Scroll to element
    /// </summary>
    protected void ScrollToElement(By locator)
    {
        var element = FindElement(locator);
        ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", element);
        Thread.Sleep(500); // Brief pause for smooth scroll
    }

    /// <summary>
    /// Get current page URL
    /// </summary>
    protected string GetCurrentUrl()
    {
        return Driver.Url;
    }

    /// <summary>
    /// Wait for URL to contain specific text
    /// </summary>
    protected void WaitForUrlContains(string urlFragment)
    {
        Wait.Until(driver => driver.Url.Contains(urlFragment));
    }

    /// <summary>
    /// Select dropdown option by visible text
    /// </summary>
    protected void SelectDropdownByText(By locator, string text)
    {
        var element = FindVisibleElement(locator);
        var select = new SelectElement(element);
        select.SelectByText(text);
    }

    /// <summary>
    /// Select dropdown option by value
    /// </summary>
    protected void SelectDropdownByValue(By locator, string value)
    {
        var element = FindVisibleElement(locator);
        var select = new SelectElement(element);
        select.SelectByValue(value);
    }

    /// <summary>
    /// Get attribute value from element
    /// </summary>
    protected string? GetAttribute(By locator, string attributeName)
    {
        return FindElement(locator).GetAttribute(attributeName);
    }

    /// <summary>
    /// Wait for a custom condition
    /// </summary>
    protected T? WaitUntil<T>(Func<IWebDriver, T> condition)
    {
        return Wait.Until(condition);
    }

    /// <summary>
    /// Refresh the current page
    /// </summary>
    protected void RefreshPage()
    {
        Driver.Navigate().Refresh();
        WaitForPageLoad();
    }
}
