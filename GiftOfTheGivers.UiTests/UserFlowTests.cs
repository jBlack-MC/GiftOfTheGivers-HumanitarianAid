using GiftOfTheGivers.UiTests.Infrastructure;
using OpenQA.Selenium;

namespace GiftOfTheGivers.UiTests;

/// <summary>
/// Browser tests of the flows the Foundation's staff and donors use, each at desktop and/or
/// mobile size, including the error paths (bad password, invalid forms, protected pages).
/// Tests marked Smoke only read data, so they are also safe to run against the live site
/// after a deployment.
/// </summary>
[Collection(UiCollection.Name)]
[Trait("Category", "UI")]
public class UserFlowTests(UiTestServer server)
{
    private static string EmployeeEmail => Environment.GetEnvironmentVariable("UI_EMPLOYEE_EMAIL") ?? "employee@giftofthegivers.org";
    private static string EmployeePassword => Environment.GetEnvironmentVariable("UI_EMPLOYEE_PASSWORD") ?? "Employee#123";
    private static string DonorEmail => Environment.GetEnvironmentVariable("UI_DONOR_EMAIL") ?? "donor@example.com";
    private static string DonorPassword => Environment.GetEnvironmentVariable("UI_DONOR_PASSWORD") ?? "Donor#123";

    private Browser Open(Viewport viewport = Viewport.Desktop) => new(server.BaseUrl, viewport);

    [Theory]
    [Trait("Category", "Smoke")]
    [InlineData(Viewport.Desktop)]
    [InlineData(Viewport.Mobile)]
    public void HomePage_Loads_WithDonateAndVolunteerLinks(Viewport viewport)
    {
        using var browser = Open(viewport);

        browser.SkipIntro();
        browser.GoTo("/");
        browser.Find(By.CssSelector("a[href*='/Home/Donate']"));

        Assert.Contains("Gift of the Givers", browser.Driver.Title, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(browser.Driver.FindElements(By.CssSelector("a[href*='/Home/Donate']")));
        Assert.NotEmpty(browser.Driver.FindElements(By.CssSelector("a[href*='/Home/Volunteer']")));
        browser.Screenshot($"01-home-{viewport.ToString().ToLowerInvariant()}");
    }

    [Fact]
    public void EmployeeLogin_ValidCredentials_OpensEmployeeDashboard()
    {
        using var browser = Open();

        browser.GoTo("/Identity/Account/Login");
        browser.Screenshot("02-login-page");
        browser.LogIn(EmployeeEmail, EmployeePassword);
        browser.WaitForText("Logout");
        browser.GoTo("/Employee/Dashboard");

        browser.WaitForText("Employee Dashboard");
        browser.Screenshot("03-employee-dashboard");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Login_WrongPassword_ShowsErrorAndStaysLoggedOut()
    {
        using var browser = Open();

        browser.LogIn(EmployeeEmail, "definitely-not-the-password");

        browser.WaitForText("Invalid login attempt");
        Assert.Contains("/Identity/Account/Login", browser.Driver.Url);
        Assert.DoesNotContain("Logout", browser.PageText);
        browser.Screenshot("04-login-error");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void AnonymousVisitor_OpeningEmployeeArea_IsSentToLogin()
    {
        using var browser = Open();

        browser.GoTo("/Employee/Dashboard");

        browser.WaitForUrl("/Identity/Account/Login");
        Assert.Contains("ReturnUrl", browser.Driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(Viewport.Desktop)]
    [InlineData(Viewport.Mobile)]
    public void AnonymousRecurringDonation_InUsDollars_ReachesReceipt(Viewport viewport)
    {
        using var browser = Open(viewport);
        var size = viewport.ToString().ToLowerInvariant();

        browser.GoTo("/Home/Donate");
        browser.Click(By.Id("recurring"));
        browser.Select(By.Id("currency"), "USD");
        browser.Type(By.Id("Amount"), "25");
        browser.WaitForText("R 462.50");                // live rand equivalent (25 × 18.50)
        browser.Type(By.Id("DonorName"), "Selenium Donor");
        browser.Type(By.Id("DonorEmail"), "selenium.donor@example.com");
        browser.Check(By.Id("IsAnonymous"));
        browser.Check(By.Id("PrivacyConsent"));
        browser.ScrollToTop();
        browser.Screenshot($"05-donate-form-top-{size}");
        browser.Script("window.scrollTo(0, document.body.scrollHeight / 2);");
        browser.Screenshot($"05-donate-form-bottom-{size}");
        browser.Click(By.CssSelector("#donateForm button[type='submit']"));

        if (server.IsSelfHosted)
        {
            browser.WaitForUrl("/Donate/Confirmation");
            browser.WaitForText("Awaiting payment");
            browser.Screenshot($"06-donation-receipt-{size}");
        }
        else
        {
            // Against Azure the PayFast sandbox takes over; reaching it is the success condition.
            browser.Wait.Until(d => d.Url.Contains("payfast", StringComparison.OrdinalIgnoreCase)
                                    || d.Url.Contains("/Donate/Confirmation", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Donation_WithoutAmountOrConsent_ShowsValidationErrors()
    {
        using var browser = Open();

        browser.GoTo("/Home/Donate");
        browser.Type(By.Id("DonorName"), "Incomplete Donor");
        browser.Type(By.Id("DonorEmail"), "not-an-email");
        browser.Click(By.CssSelector("#donateForm button[type='submit']"));

        browser.Wait.Until(d => d.FindElements(By.CssSelector(".field-validation-error, .validation-summary-errors li"))
            .Any(e => e.Displayed && e.Text.Length > 0));
        Assert.Contains("/Home/Donate", browser.Driver.Url);
        browser.ScrollToTop();
        browser.Screenshot("07-donate-validation");
    }

    [Fact]
    public void VolunteerRegistration_ShowsConfirmation()
    {
        using var browser = Open();

        browser.GoTo("/Home/Volunteer");
        browser.Type(By.Id("FullName"), "Naledi Selenium");
        browser.Type(By.Id("Email"), $"naledi.{Guid.NewGuid():N}@example.com");
        browser.Type(By.Id("PhoneNumber"), "082 123 4567");
        browser.Type(By.Id("City"), "Johannesburg");
        browser.Type(By.Id("Skills"), "First aid, driving, logistics");
        browser.Type(By.Id("Availability"), "Weekends");
        browser.Check(By.Id("PrivacyConsent"));
        browser.ScrollToTop();
        browser.Screenshot("08-volunteer-form");
        browser.Click(By.CssSelector("form[action*='Volunteer'] button[type='submit']"));

        browser.WaitForText("THANK YOU FOR VOLUNTEERING");
        browser.Screenshot("09-volunteer-confirmation");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void VolunteerRegistration_MissingRequiredFields_IsRejected()
    {
        using var browser = Open();

        browser.GoTo("/Home/Volunteer");
        browser.Click(By.CssSelector("form[action*='Volunteer'] button[type='submit']"));

        browser.Wait.Until(d => d.FindElements(By.CssSelector(".field-validation-error"))
            .Count(e => e.Displayed && e.Text.Length > 0) >= 2);
        Assert.DoesNotContain("THANK YOU FOR VOLUNTEERING", browser.PageText);
    }

    [Fact]
    public void Employee_PostsProjectUpdate()
    {
        using var browser = Open();
        var title = "Selenium update " + DateTime.Now.ToString("HHmmss");

        browser.LogIn(EmployeeEmail, EmployeePassword);
        browser.WaitForText("Logout");
        browser.GoTo("/Employee/ReliefProjects");
        browser.Screenshot("10-employee-projects");
        browser.Click(By.CssSelector("a[href*='CreateProjectUpdate']"));
        browser.Type(By.Id("Title"), title);
        browser.Type(By.Id("Content"), "Water purification tablets delivered to 120 households.");
        browser.Screenshot("11-employee-post-update");
        browser.Click(By.CssSelector("form[action*='CreateProjectUpdate'] button[type='submit']"));

        browser.WaitForText("Project update created successfully");
        browser.Screenshot("12-employee-update-posted");
    }

    [Fact]
    public void Donor_OpensTaxCertificateForVerifiedDonation()
    {
        using var browser = Open();

        browser.LogIn(DonorEmail, DonorPassword);
        browser.WaitForText("Logout");
        browser.GoTo("/Donor/Dashboard");
        browser.WaitForText("Donor Dashboard");
        browser.Screenshot("13-donor-dashboard");
        browser.GoTo("/Donor/MyDonations");
        browser.WaitForUrl("/Donor/MyDonations");
        browser.Find(By.CssSelector("a[href*='/Donor/TaxCertificate/']"));
        browser.Screenshot("14-donor-my-donations");
        browser.Click(By.CssSelector("a[href*='/Donor/TaxCertificate/']"));

        browser.Find(By.CssSelector("a[href*='DownloadTaxCertificate']"));
        browser.Screenshot("15-donor-tax-certificate");
    }
}
