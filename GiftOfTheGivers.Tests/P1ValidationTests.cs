using System.ComponentModel.DataAnnotations;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ValidationAttributes;
using GiftOfTheGivers.Services;

public class P1ValidationTests
{
    [Fact]
    public void AllowedImageUrl_RejectsJavascriptScheme()
    {
        var attribute = new AllowedImageUrlAttribute();
        var result = attribute.GetValidationResult("javascript:alert(1)", new ValidationContext(new object()));

        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void StatusEnums_DefaultToSafeValues()
    {
        Assert.Equal(ProjectStatus.Active, new ReliefProject().Status);
        Assert.Equal(VolunteerStatus.Pending, new Volunteer().Status);
    }

    [Fact]
    public void TaxCertificatePdf_StartsWithPdfMagicBytes()
    {
        var bytes = TaxCertificatePdf.Generate(new Donation
        {
            Amount = 500,
            Currency = "ZAR",
            TransactionReference = "TEST-REF-001",
            DonationDate = DateTime.UtcNow
        });

        Assert.True(bytes.Length > 5);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }
}