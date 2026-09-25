using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GiftOfTheGivers.Tests;

public class DatabaseContextTests
{
	[Fact]
	public async Task ContextCanPersistAndReadDonationWithInMemoryProvider()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ApplicationDbContext(options);
		var donation = new Donation { Amount = 125m, Currency = "ZAR" };

		context.Donations.Add(donation);
		await context.SaveChangesAsync();

		var savedDonation = await context.Donations.SingleAsync();
		Assert.Equal(donation.Id, savedDonation.Id);
		Assert.Equal(125m, savedDonation.Amount);
	}
}
