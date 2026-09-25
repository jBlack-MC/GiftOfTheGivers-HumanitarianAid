using GiftOfTheGivers.Helpers;
using Xunit;

namespace GiftOfTheGivers.Tests;

public class HelperCalculationsTests
{
    [Theory]
    [InlineData(50_000, 100_000, false, 50)]
    [InlineData(150_000, 100_000, true, 100)]
    [InlineData(150_000, 100_000, false, 150)]
    [InlineData(500, 0, false, 0)]
    public void FundingPercentage_UsesSharedCalculation(
        decimal raised,
        decimal required,
        bool clampTo100,
        int expected)
    {
        Assert.Equal(expected, FundingCalculations.FundingPercentage(raised, required, clampTo100));
    }

    [Fact]
    public void DonationCalculations_ReturnTotalAndAverage()
    {
        var amounts = new[] { 10m, 20m, 30m };

        Assert.Equal(60m, DonationCalculations.TotalAmount(amounts));
        Assert.Equal(20m, DonationCalculations.AverageAmount(amounts));
    }

    [Fact]
    public void DonationCalculations_ReturnZeroForEmptyAverage()
    {
        Assert.Equal(0m, DonationCalculations.AverageAmount(Array.Empty<decimal>()));
    }

    [Fact]
    public void TaxCertificateNumber_UsesReferenceOrFormattedDonationId()
    {
        Assert.Equal("PAYMENT-42", TaxCertificateNumber.Format("PAYMENT-42", 42));
        Assert.Equal("GOTG-00000042", TaxCertificateNumber.Format(null, 42));
    }
}