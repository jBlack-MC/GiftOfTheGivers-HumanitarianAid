using System.Text;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Xunit;

namespace GiftOfTheGivers.Tests;

public class TaxCertificatePdfTests
{
	[Fact]
	public void Generate_ProducesPdfMagicBytes()
	{
		var pdf = TaxCertificatePdf.Generate(new Donation
		{
			Id = 42,
			Amount = 500m,
			Currency = "ZAR",
			TransactionReference = "TEST-REF-42",
			DonationDate = new DateTime(2026, 9, 25)
		});

		Assert.True(pdf.Length > 5);
		Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
	}
}
