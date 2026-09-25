using System.Globalization;

namespace GiftOfTheGivers.Helpers;

public static class TaxCertificateNumber
{
    public static string Format(string? transactionReference, int donationId)
    {
        return transactionReference ?? $"GOTG-{donationId.ToString("D8", CultureInfo.InvariantCulture)}";
    }
}