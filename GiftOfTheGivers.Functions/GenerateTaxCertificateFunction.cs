using System.Net;
using System.Text.RegularExpressions;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Functions;

/// <summary>
/// Server-to-server certificate generation (e.g. for a batch emailer). It is protected by a
/// function key (<see cref="AuthorizationLevel.Function"/>), looks donations up by their
/// unguessable 32-character transaction reference rather than the sequential id, and only
/// issues certificates for verified (paid) donations. Donor-facing downloads go through the
/// web app, which also enforces ownership.
/// </summary>
public sealed partial class GenerateTaxCertificateFunction(
    ApplicationDbContext context,
    IOptions<OrganisationOptions> organisation,
    ILogger<GenerateTaxCertificateFunction> logger)
{
    [GeneratedRegex("^[A-Za-z0-9]{16,100}$")]
    private static partial Regex ReferencePattern();

    [Function(nameof(GenerateTaxCertificate))]
    public async Task<HttpResponseData> GenerateTaxCertificate(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tax-certificate/{reference}")]
        HttpRequestData request,
        string reference)
    {
        if (!ReferencePattern().IsMatch(reference))
        {
            logger.LogWarning("Rejected malformed tax certificate reference.");
            var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("A valid donation reference is required.");
            return badRequest;
        }

        try
        {
            var donation = await context.Donations
                .AsNoTracking()
                .Include(item => item.Donor)
                .Include(item => item.ReliefProject)
                .FirstOrDefaultAsync(item => item.TransactionReference == reference
                    && item.PaymentStatus == PaymentStatus.Verified);

            if (donation is null)
            {
                logger.LogInformation("No verified donation found for a tax certificate request.");
                var notFound = request.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Donation not found.");
                return notFound;
            }

            var pdf = TaxCertificatePdf.Generate(donation, CertificateIssuer.From(organisation.Value));
            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            response.Headers.Add("Content-Disposition", $"attachment; filename=TaxCertificate_{reference}.pdf");
            await response.Body.WriteAsync(pdf.AsMemory());

            logger.LogInformation("Generated tax certificate for donation {DonationId}.", donation.Id);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to generate a tax certificate.");
            var error = request.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Unable to generate the tax certificate.");
            return error;
        }
    }
}
