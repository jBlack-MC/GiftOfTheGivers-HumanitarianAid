using GiftOfTheGivers.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GiftOfTheGivers.Services
{
    /// <summary>
    /// Builds the placeholder Section 18A tax certificate as a PDF.
    /// Part 1 prototype: the figures are symbolic and this is not a valid
    /// tax document.
    /// </summary>
    public static class TaxCertificatePdf
    {
        private static readonly string Navy = "#16294D";
        private static readonly string Accent = "#E8384F";

        public static byte[] Generate(Donation donation)
        {
            var donorName = donation.IsAnonymous || donation.Donor is null
                ? "Anonymous Donor"
                : (string.IsNullOrWhiteSpace(donation.Donor.FullName)
                    ? donation.Donor.UserName ?? "Donor"
                    : donation.Donor.FullName);

            var reference = donation.TransactionReference ?? $"GOTG-{donation.Id:D8}";
            var project = donation.ReliefProject?.Title ?? "General Fund";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(48);
                    page.DefaultTextStyle(t => t.FontSize(11).FontColor(Colors.Grey.Darken3).FontFamily("Arial"));

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
                            text.Span(" made the following donation to the Gift of the Givers Foundation, a registered Public Benefit Organisation.");
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
                            Row("Date issued", DateTime.Now.ToString("yyyy-MM-dd"));
                            Row("Donation date", donation.DonationDate.ToString("yyyy-MM-dd"));
                            Row("Amount", $"{donation.Currency} {donation.Amount:N2}");
                            Row("Donation type", donation.DonationType);
                            Row("Payment method", donation.PaymentMethod);
                            Row("Allocated to", project);
                            Row("Received with thanks by", "Gift of the Givers Foundation");
                        });

                        body.Item().PaddingTop(6).Text(
                            "Issued in terms of section 18A of the Income Tax Act. Retain this certificate for your tax records.")
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
                        footer.Item().PaddingTop(6).Text(
                            "Portfolio of Evidence Part 1 prototype — symbolic values, not a valid tax document.")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
