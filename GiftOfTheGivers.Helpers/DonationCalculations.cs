namespace GiftOfTheGivers.Helpers;

public static class DonationCalculations
{
    public static decimal TotalAmount(IEnumerable<decimal> amounts)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        return amounts.Sum();
    }

    public static decimal AverageAmount(IEnumerable<decimal> amounts)
    {
        ArgumentNullException.ThrowIfNull(amounts);

        var count = 0;
        var total = 0m;
        foreach (var amount in amounts)
        {
            total += amount;
            count++;
        }

        return count == 0 ? 0m : total / count;
    }
}