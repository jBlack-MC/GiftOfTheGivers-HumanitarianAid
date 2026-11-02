namespace GiftOfTheGivers.Models
{
    /// <summary>
    /// Settlement state of a <see cref="Donation"/>.
    ///
    /// A donation is recorded as <see cref="Pending"/> when the donor submits the form and stays
    /// that way until PayFast's Instant Transaction Notification (ITN) confirms the payment, at
    /// which point <c>DonationService.ApplyPaymentNotificationAsync</c> marks it
    /// <see cref="Verified"/>.
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
