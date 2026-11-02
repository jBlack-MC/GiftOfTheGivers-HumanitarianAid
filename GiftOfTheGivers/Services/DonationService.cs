using System.Globalization;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Services;

public enum CaptureOutcome
{
    Created,
    AlreadySubmitted,
    ProjectUnavailable
}

public sealed record CaptureResult(CaptureOutcome Outcome, Donation? Donation);

public enum NotificationOutcome
{
    Verified,
    AlreadyVerified,
    MarkedFailed,
    Ignored,
    UnknownReference,
    MerchantMismatch,
    AmountMismatch
}

/// <summary>Fields of a PayFast ITN that the donation workflow acts on.</summary>
public sealed record PaymentNotification(
    string Reference,
    string Status,
    string? AmountGross,
    string? MerchantId,
    string? ProviderPaymentId);

public interface IDonationService
{
    /// <summary>
    /// Records a donation as Pending. Never changes project totals: money only counts once the
    /// payment provider confirms it.
    /// </summary>
    Task<CaptureResult> CaptureAsync(DonateViewModel model, string? donorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a signature-verified PayFast notification. Idempotent: replays and duplicates
    /// never add the amount twice.
    /// </summary>
    Task<(NotificationOutcome Outcome, Donation? Donation)> ApplyPaymentNotificationAsync(
        PaymentNotification notification, string expectedMerchantId, CancellationToken cancellationToken = default);

    /// <summary>Sets FundsRaised = offline funds + verified online donations for one project.</summary>
    Task<(decimal Before, decimal After)> RecalculateFundsRaisedAsync(int projectId, CancellationToken cancellationToken = default);
}

public sealed class DonationService(
    ApplicationDbContext context,
    ICurrencyConverter currencyConverter,
    IAuditService auditService,
    ILogger<DonationService> logger) : IDonationService
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<CaptureResult> CaptureAsync(
        DonateViewModel model, string? donorUserId, CancellationToken cancellationToken = default)
    {
        var existing = await FindByIdempotencyKeyAsync(model.IdempotencyToken, cancellationToken);
        if (existing is not null)
        {
            return new CaptureResult(CaptureOutcome.AlreadySubmitted, existing);
        }

        ReliefProject? project = null;
        if (model.ReliefProjectId.HasValue)
        {
            project = await context.ReliefProjects
                .FirstOrDefaultAsync(p => p.Id == model.ReliefProjectId.Value, cancellationToken);
            if (project is null || project.Status != ProjectStatus.Active)
            {
                return new CaptureResult(CaptureOutcome.ProjectUnavailable, null);
            }
        }

        var (amountZar, rate) = currencyConverter.ToZar(model.Amount, model.Currency);
        var donation = new Donation
        {
            Amount = model.Amount,
            AmountZar = amountZar,
            ExchangeRateToZar = rate,
            Currency = model.Currency,
            DonationType = model.DonationType,
            PaymentMethod = model.PaymentMethod,
            DonationDate = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            IsAnonymous = model.IsAnonymous,
            DonorName = model.DonorName.Trim(),
            DonorEmail = model.DonorEmail.Trim(),
            ReliefProjectId = project?.Id,
            ReliefProject = project,
            TransactionReference = Guid.NewGuid().ToString("N"),
            IdempotencyKey = model.IdempotencyToken,
            PaymentStatus = PaymentStatus.Pending,
            DonorId = donorUserId,
            PrivacyConsentAt = DateTime.UtcNow
        };

        context.Donations.Add(donation);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A double-click can race the lookup above; the unique index on IdempotencyKey wins
            // and the first donation is the one the donor sees.
            context.Entry(donation).State = EntityState.Detached;
            var winner = await FindByIdempotencyKeyAsync(model.IdempotencyToken, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return new CaptureResult(CaptureOutcome.AlreadySubmitted, winner);
        }

        return new CaptureResult(CaptureOutcome.Created, donation);
    }

    public async Task<(NotificationOutcome Outcome, Donation? Donation)> ApplyPaymentNotificationAsync(
        PaymentNotification notification, string expectedMerchantId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            var donation = await context.Donations
                .Include(d => d.ReliefProject)
                .FirstOrDefaultAsync(d => d.TransactionReference == notification.Reference, cancellationToken);

            if (donation is null)
            {
                return (NotificationOutcome.UnknownReference, null);
            }

            if (!string.Equals(notification.MerchantId, expectedMerchantId, StringComparison.Ordinal))
            {
                logger.LogWarning("PayFast ITN for {Reference} carried merchant id {MerchantId}, expected {Expected}",
                    notification.Reference, notification.MerchantId, expectedMerchantId);
                return (NotificationOutcome.MerchantMismatch, donation);
            }

            if (donation.PaymentStatus == PaymentStatus.Verified)
            {
                return (NotificationOutcome.AlreadyVerified, donation);
            }

            if (notification.Status == "COMPLETE")
            {
                if (!decimal.TryParse(notification.AmountGross, NumberStyles.Number, CultureInfo.InvariantCulture, out var paid)
                    || paid != donation.AmountZar)
                {
                    logger.LogError("PayFast ITN amount {Paid} does not match donation {Reference} amount {Expected}",
                        notification.AmountGross, notification.Reference, donation.AmountZar);
                    return (NotificationOutcome.AmountMismatch, donation);
                }

                donation.PaymentStatus = PaymentStatus.Verified;
                donation.PaymentVerifiedAt = DateTime.UtcNow;
                donation.ProviderPaymentId = notification.ProviderPaymentId;
                donation.TaxCertificateIssued = true;
                if (donation.ReliefProject is not null)
                {
                    donation.ReliefProject.FundsRaised += donation.AmountZar;
                }

                auditService.Record(AuditLog.SystemPayFast, "PaymentVerified", nameof(Donation), donation.Id,
                    $"R {donation.AmountZar:F2} verified (pf_payment_id {notification.ProviderPaymentId}).");
            }
            else if (donation.PaymentStatus == PaymentStatus.Pending)
            {
                donation.PaymentStatus = PaymentStatus.Failed;
                donation.ProviderPaymentId = notification.ProviderPaymentId;
                auditService.Record(AuditLog.SystemPayFast, "PaymentFailed", nameof(Donation), donation.Id,
                    $"PayFast status {notification.Status}.");
            }
            else
            {
                return (NotificationOutcome.Ignored, donation);
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return (donation.PaymentStatus == PaymentStatus.Verified
                    ? NotificationOutcome.Verified
                    : NotificationOutcome.MarkedFailed, donation);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // Another payment for the same project committed first; reload and re-apply.
                context.ChangeTracker.Clear();
            }
        }
    }

    public async Task<(decimal Before, decimal After)> RecalculateFundsRaisedAsync(
        int projectId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            var project = await context.ReliefProjects.FirstAsync(p => p.Id == projectId, cancellationToken);
            var verified = await context.Donations
                .Where(d => d.ReliefProjectId == projectId && d.PaymentStatus == PaymentStatus.Verified)
                .SumAsync(d => (decimal?)d.AmountZar, cancellationToken) ?? 0m;

            var before = project.FundsRaised;
            project.FundsRaised = project.OfflineFundsZar + verified;
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return (before, project.FundsRaised);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                context.ChangeTracker.Clear();
            }
        }
    }

    private Task<Donation?> FindByIdempotencyKeyAsync(Guid key, CancellationToken cancellationToken) =>
        context.Donations
            .Include(d => d.ReliefProject)
            .FirstOrDefaultAsync(d => d.IdempotencyKey == key, cancellationToken);
}
