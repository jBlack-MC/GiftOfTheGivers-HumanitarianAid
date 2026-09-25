using GiftOfTheGivers.Models;
using Xunit;

namespace GiftOfTheGivers.Tests;

public class ModelTests
{
	[Theory]
	[InlineData(50_000, 100_000, false, 50)]
	[InlineData(150_000, 100_000, true, 100)]
	[InlineData(150_000, 100_000, false, 150)]
	[InlineData(500, 0, false, 0)]
	public void FundingPercentage_PreservesExistingValues(
		decimal raised,
		decimal required,
		bool clampTo100,
		int expected)
	{
		var project = new ReliefProject { FundsRaised = raised, FundsRequired = required };

		Assert.Equal(expected, project.FundingPercentage(clampTo100));
	}

	[Fact]
	public void FundingPercentage_ReturnsZeroForNullProject()
	{
		ReliefProject? project = null;

		Assert.Equal(0, project.FundingPercentage());
	}
}
