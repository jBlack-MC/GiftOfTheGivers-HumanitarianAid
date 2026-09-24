namespace GiftOfTheGivers.Models
{
    /// <summary>
    /// Settlement state of a <see cref="Donation"/>.
    ///
    /// A donation is recorded as <see cref="Pending"/> and stays that way until a
    /// payment provider confirms the money actually arrived. Nothing in this prototype
    /// marks a donation <see cref="Verified"/> (no gateway is wired up yet), so the
    /// system no longer implies that every captured donation is settled.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>Captured locally, awaiting confirmation from the payment provider.</summary>
        Pending = 0,

        /// <summary>Confirmed by the payment provider; the money is in the bank.</summary>
        Verified = 1,

        /// <summary>Rejected, cancelled, reversed or timed out.</summary>
        Failed = 2
    }
}
