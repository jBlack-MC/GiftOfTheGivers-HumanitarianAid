namespace GiftOfTheGivers.Helpers;

public static class FundingCalculations
{
    public static int FundingPercentage(decimal fundsRaised, decimal fundsRequired, bool clampTo100 = false)
    {
        if (fundsRequired <= 0)
        {
            return 0;
        }

        var percentage = (int)((fundsRaised / fundsRequired) * 100);
        return clampTo100 ? Math.Min(percentage, 100) : percentage;
    }
}