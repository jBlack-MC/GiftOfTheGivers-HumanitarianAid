using GiftOfTheGivers.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GiftOfTheGivers.Services
{
    /// <summary>Who issues a certificate. Comes from the <c>Organisation</c> configuration section.</summary>
    public sealed record CertificateIssuer(string OrganisationName, string PboReference, bool IsOfficial)
    {
        public static CertificateIssuer Demo { get; } = new("Gift of the Givers Foundation", string.Empty, false);

        public static CertificateIssuer From(OrganisationOptions options) =>
            new(options.Name, options.PboReference, options.CertificatesAreOfficial);
    }

    /// <summary>
    /// Builds the Section 18A donation certificate as a PDF. Callers must only pass
    /// <see cref="PaymentStatus.Verified"/> donations. Until the organisation's PBO reference is
    /// configured and demo mode is off, the certificate is clearly marked as a demonstration.
    /// </summary>
    public static class TaxCertificatePdf
    {
        private static readonly string Navy = "#16294D";
        private static readonly string Accent = "#E8384F";

        public static byte[] Generate(Donation donation) => Generate(donation, CertificateIssuer.Demo);

        public static string DonorDisplayName(Donation donation)
        {
            if (donation.IsAnonymous)
            {
                return "Anonymous Donor";
            }

            if (!string.IsNullOrWhiteSpace(donation.Donor?.FullName))
            {
                return donation.Donor.FullName;
            }

            return string.IsNullOrWhiteSpace(donation.DonorName) ? "Donor" : donation.DonorName;
        }

        public static byte[] Generate(Donation donation, CertificateIssuer issuer)
        {
            var donorName = DonorDisplayName(donation);

            var reference = GiftOfTheGivers.Helpers.TaxCertificateNumber.Format(
                donation.TransactionReference,
                donation.Id);
            var project = donation.ReliefProject?.Title ?? "General Fund";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(48);
                    page.DefaultTextStyle(t => t.FontSize(11).FontColor(Colors.Grey.Darken3));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("GIFT OF THE GIVERS").FontSize(22).Bold().FontColor(Navy);
                        header.Item().Text("Relief Management System").FontSize(10).FontColor(Colors.Grey.Medium);
                        header.Item().PaddingTop(4).LineHorizontal(2).LineColor(Accent);
                        header.Item().PaddingTop(12).Text("Section 18A Donation Tax Certificate")
                            .FontSize(15).SemiBold().FontColor(Navy);
                    });

                    page.Content().PaddingVertical(20).Column(body =>
                    {
                        body.Spacing(14);

                        body.Item().Text(text =>
                        {
                            text.Span("This certifies that ");
                            text.Span(donorName).SemiBold();
                            text.Span($" made the following donation to {issuer.OrganisationName}.");
                        });

                        body.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(170);
                                c.RelativeColumn();
                            });

                            void Row(string label, string value)
                            {
                                table.Cell().PaddingVertical(5).Text(label).FontColor(Colors.Grey.Darken1);
                                table.Cell().PaddingVertical(5).Text(value).SemiBold();
                            }

                            Row("Certificate number", reference);
                            Row("Date issued", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                            Row("PBO reference", string.IsNullOrWhiteSpace(issuer.PboReference) ? "Not configured" : issuer.PboReference);
                            Row("Donation date", donation.DonationDate.ToString("yyyy-MM-dd"));
                            Row("Amount", donation.Currency == "ZAR"
                                ? $"ZAR {donation.Amount:N2}"
                                : $"{donation.Currency} {donation.Amount:N2} (ZAR {donation.AmountZar:N2})");
                            Row("Donation type", donation.DonationType);
                            Row("Payment method", donation.PaymentMethod);
                            Row("Allocated to", project);
                            Row("Received with thanks by", issuer.OrganisationName);
                        });

                        body.Item().PaddingTop(6).Text(issuer.IsOfficial
                                ? "Issued in terms of section 18A of the Income Tax Act. Retain this certificate for your tax records."
                                : "DEMONSTRATION ONLY: this document has the layout of a section 18A certificate but is not valid for tax purposes.")
                            .FontColor(Colors.Grey.Darken1);

                        body.Item().PaddingTop(30).Row(row =>
                        {
                            row.RelativeItem().Column(sig =>
                            {
                                sig.Item().Width(180).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                sig.Item().PaddingTop(3).Text("Authorised signatory").FontSize(9).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(sig =>
                            {
                                sig.Item().Width(180).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                sig.Item().PaddingTop(3).Text("Date").FontSize(9).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        footer.Item().PaddingTop(6).Text(issuer.IsOfficial
                                ? $"{issuer.OrganisationName} · PBO {issuer.PboReference}"
                                : "Demonstration certificate — not valid for tax purposes until the organisation's PBO details are configured.")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
