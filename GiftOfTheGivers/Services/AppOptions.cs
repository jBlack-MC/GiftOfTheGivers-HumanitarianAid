namespace GiftOfTheGivers.Services;

/// <summary>
/// Organisation details shown on the site and printed on tax certificates. Bound from the
/// <c>Organisation</c> configuration section so that no real organisation's registration
/// numbers or contact details are hard-coded in views.
/// </summary>
public sealed class OrganisationOptions
{
    public string Name { get; set; } = "Gift of the Givers Foundation";
    public string ContactEmail { get; set; } = "info@example.org";
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>Section 18A Public Benefit Organisation reference. Empty = not configured.</summary>
    public string PboReference { get; set; } = string.Empty;

    /// <summary>POPIA Information Officer contact for privacy requests.</summary>
    public string InformationOfficerEmail { get; set; } = string.Empty;

    /// <summary>
    /// True while the site is a demonstration. Certificates and contact details are labelled as
    /// demonstration content until this is switched off and the fields above are populated.
    /// </summary>
    public bool IsDemo { get; set; } = true;

    public bool CertificatesAreOfficial => !IsDemo && !string.IsNullOrWhiteSpace(PboReference);
}

/// <summary>Exchange rates used to convert foreign-currency donations to rand.</summary>
public sealed class CurrencyOptions
{
    /// <summary>
    /// Rand value of one unit of each supported currency. These are indicative values that must
    /// be maintained (or replaced with a rates provider) before accepting real foreign donations.
    /// </summary>
    public Dictionary<string, decimal> RatesToZar { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Controls how staff (Employee role) accounts are provisioned.</summary>
public sealed class StaffAccessOptions
{
    /// <summary>
    /// Existing accounts that are granted the Employee role at startup. This is the only way to
    /// create the first staff account; afterwards employees grant access from Staff Access.
    /// </summary>
    public string[] BootstrapEmployeeEmails { get; set; } = [];
}

/// <summary>
/// Short-lived caching of the public, read-mostly figures on the home page. Added after the
/// Part 3 stress test showed the home page (six database queries per view) was the bottleneck.
/// 0 turns caching off (used by the tests so figures update immediately).
/// </summary>
public sealed class CachingOptions
{
    public int PublicPageSeconds { get; set; } = 30;
}
