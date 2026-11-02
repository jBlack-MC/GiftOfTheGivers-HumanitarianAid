using System.Globalization;

namespace GiftOfTheGivers.Infrastructure;

/// <summary>
/// Culture-independent money formatting. <c>ToString("C")</c> uses the server's culture, which
/// renders "$" or "¤" for rand amounts on Linux containers and Azure App Service.
/// </summary>
public static class Money
{
    public static string Symbol(string? currency) => currency switch
    {
        "USD" => "$",
        "EUR" => "€",
        _ => "R"
    };

    public static string Format(decimal amount, string? currency = "ZAR", int decimals = 2) =>
        $"{Symbol(currency)} {amount.ToString(decimals == 0 ? "#,##0" : "#,##0.00", CultureInfo.InvariantCulture)}";

    public static string Zar(decimal amount, int decimals = 2) => Format(amount, "ZAR", decimals);
}
