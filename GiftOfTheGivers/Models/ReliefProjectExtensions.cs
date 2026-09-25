using GiftOfTheGivers.Helpers;

namespace GiftOfTheGivers.Models;

public static class ReliefProjectExtensions
{
	public static int FundingPercentage(this ReliefProject? project, bool clampTo100 = false)
	{
		return FundingCalculations.FundingPercentage(
			project?.FundsRaised ?? 0m,
			project?.FundsRequired ?? 0m,
			clampTo100);
	}
}
