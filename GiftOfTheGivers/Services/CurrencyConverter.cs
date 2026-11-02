using GiftOfTheGivers.Models;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Services;

public interface ICurrencyConverter
{
    /// <summary>Rand value of one unit of each supported currency.</summary>
    IReadOnlyDictionary<string, decimal> RatesToZar { get; }

    /// <summary>Converts an amount to rand, rounded to cents.</summary>
    (decimal AmountZar, decimal Rate) ToZar(decimal amount, string currency);
}

public sealed class CurrencyConverter : ICurrencyConverter
{
    public CurrencyConverter(IOptions<CurrencyOptions> options)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["ZAR"] = 1m };
        foreach (var (currency, rate) in options.Value.RatesToZar)
        {
            if (rate > 0m)
            {
                rates[currency.ToUpperInvariant()] = rate;
            }
        }

        var missing = Donation.SupportedCurrencies.Where(c => !rates.ContainsKey(c)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Currency:RatesToZar is missing a positive rate for: {string.Join(", ", missing)}.");
        }

        RatesToZar = rates;
    }

    public IReadOnlyDictionary<string, decimal> RatesToZar { get; }

    public (decimal AmountZar, decimal Rate) ToZar(decimal amount, string currency)
    {
        if (!RatesToZar.TryGetValue(currency, out var rate))
        {
            throw new ArgumentException($"Unsupported currency '{currency}'.", nameof(currency));
        }

        return (Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero), rate);
    }
}
