using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace GiftOfTheGivers.UiTests.Infrastructure;

/// <summary>Screen sizes every responsive test runs at.</summary>
public enum Viewport
{
    Desktop,
    Mobile
}

/// <summary>
/// One browser session per test. Configuration comes from environment variables so the same
/// tests run in Visual Studio, on a build agent and against Azure:
///   UI_BROWSER         chrome (default) | edge | firefox
///   UI_HEADLESS        true (default) | false to watch the browser
///   UI_SCREENSHOT_DIR  where step screenshots are saved (default: ./screenshots)
/// Selenium Manager downloads the matching driver automatically.
/// </summary>
public sealed class Browser : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public Browser(string baseUrl, Viewport viewport = Viewport.Desktop)
    {
        BaseUrl = baseUrl;
        BrowserName = (Environment.GetEnvironmentVariable("UI_BROWSER") ?? "chrome").ToLowerInvariant();
        var headless = !string.Equals(Environment.GetEnvironmentVariable("UI_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);
        Driver = CreateDriver(BrowserName, headless);
        Driver.Manage().Window.Size = viewport == Viewport.Mobile ? new Size(390, 844) : new Size(1366, 900);
        Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        Wait = new WebDriverWait(Driver, Timeout);
        // Pages are replaced during navigation; an element read mid-navigation is simply retried.
        Wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));
        ScreenshotDirectory = Environment.GetEnvironmentVariable("UI_SCREENSHOT_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(ScreenshotDirectory);
    }

    public IWebDriver Driver { get; }
    public WebDriverWait Wait { get; }
    public string BaseUrl { get; }
    public string BrowserName { get; }
    public string ScreenshotDirectory { get; }

    private static IWebDriver CreateDriver(string browser, bool headless)
    {
        switch (browser)
        {
            case "edge":
                var edge = new EdgeOptions { PageLoadStrategy = PageLoadStrategy.Eager };
                if (headless) { edge.AddArgument("--headless=new"); }
                edge.AddArgument("--disable-gpu");
                return new EdgeDriver(edge);
            case "firefox":
                var firefox = new FirefoxOptions { PageLoadStrategy = PageLoadStrategy.Eager };
                if (headless) { firefox.AddArgument("-headless"); }
                return new FirefoxDriver(firefox);
            default:
                var chrome = new ChromeOptions { PageLoadStrategy = PageLoadStrategy.Eager };
                if (headless) { chrome.AddArgument("--headless=new"); }
                chrome.AddArguments("--no-sandbox", "--disable-dev-shm-usage", "--disable-gpu");
                return new ChromeDriver(chrome);
        }
    }

    public void GoTo(string path) => Driver.Navigate().GoToUrl(BaseUrl + path);

    public IWebElement Find(By by) => Wait.Until(d => d.FindElement(by));

    public void Type(By by, string text)
    {
        var element = Find(by);
        element.Clear();
        element.SendKeys(text);
    }

    /// <summary>Scrolls the element to the middle of the screen first so sticky headers never cover it.</summary>
    public void Click(By by) => Wait.Until(d =>
    {
        // Re-find on every attempt: the page may still be replacing the DOM after a navigation.
        try
        {
            Click(d.FindElement(by));
            return true;
        }
        catch (Exception exception) when (exception is StaleElementReferenceException or NoSuchElementException)
        {
            return false;
        }
    });

    public void Click(IWebElement element)
    {
        Script("arguments[0].scrollIntoView({block: 'center'});", element);
        Wait.Until(_ => element.Displayed && element.Enabled);
        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // A floating element (e.g. the back-to-top button) overlaps it at this screen size.
            Script("arguments[0].click();", element);
        }
    }

    public object? Script(string script, params object[] args) => ((IJavaScriptExecutor)Driver).ExecuteScript(script, args);

    public void ScrollToTop() => Script("window.scrollTo(0, 0);");

    /// <summary>
    /// The first visit to the home page in a browser session plays the animated intro. Mark it as
    /// seen so tests land on the real home page.
    /// </summary>
    public void SkipIntro()
    {
        GoTo("/Home/Privacy");
        Script("sessionStorage.setItem('gotg-intro-seen', '1');");
    }

    public void Select(By by, string value) => Find(by).FindElement(By.CssSelector($"option[value='{value}']")).Click();

    public void Check(By by)
    {
        var box = Find(by);
        if (!box.Selected) { Click(box); }
    }

    public string PageText => Driver.FindElement(By.TagName("body")).Text;

    public void WaitForUrl(string fragment) => Wait.Until(d => d.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    public void WaitForText(string text) =>
        Wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains(text, StringComparison.OrdinalIgnoreCase));

    /// <summary>Saves a labelled screenshot (used as evidence and in the user manual).</summary>
    public string Screenshot(string name)
    {
        var file = Path.Combine(ScreenshotDirectory, $"{name}-{BrowserName}.png");
        ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile(file);
        return file;
    }

    public void LogIn(string email, string password)
    {
        GoTo("/Identity/Account/Login");
        Type(By.Id("Input_Email"), email);
        Type(By.Id("Input_Password"), password);
        Click(By.Id("login-submit"));
        // Wait for the result of the POST: either we leave the login page or it shows an error.
        Wait.Until(d => !d.Url.Contains("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase)
                        || d.FindElements(By.CssSelector(".validation-summary-errors li, .text-danger li")).Count > 0);
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}
