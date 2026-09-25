using System.Globalization;
using System.Net;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftOfTheGivers.Functions;

public sealed class GenerateTaxCertificateFunction(
    ApplicationDbContext context,
    ILogger<GenerateTaxCertificateFunction> logger)
{
    [Function(nameof(GenerateTaxCertificate))]
    public async Task<HttpResponseData> GenerateTaxCertificate(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tax-certificates/{donationId}")]
        HttpRequestData request,
        string donationId)
    {
        logger.LogInformation("Received tax certificate request for donation {DonationId}.", donationId);

        if (!int.TryParse(donationId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
        {
            logger.LogWarning("Rejected invalid tax certificate donation ID {DonationId}.", donationId);
            var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Donation ID must be a positive integer.");
            return badRequest;
        }

        try
        {
            var donation = await context.Donations
                .AsNoTracking()
                .Include(item => item.Donor)
                .Include(item => item.ReliefProject)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (donation is null)
            {
                logger.LogInformation("No donation found for tax certificate request {DonationId}.", id);
                var notFound = request.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Donation not found.");
                return notFound;
            }

            var pdf = TaxCertificatePdf.Generate(donation);
            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            response.Headers.Add("Content-Disposition", $"attachment; filename=TaxCertificate_{id}.pdf");
            await response.Body.WriteAsync(pdf.AsMemory());

            logger.LogInformation("Generated tax certificate for donation {DonationId}.", id);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to generate tax certificate for donation {DonationId}.", id);
            var error = request.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Unable to generate the tax certificate.");
            return error;
        }
    }
}