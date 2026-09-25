using System.Net;
using System.Security.Cryptography;
using System.Text;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace GiftOfTheGivers.Tests;

public class PayFastPaymentServiceTests
{
    [Fact]
    public async Task VerifyNotificationAsync_PreservesPostedFieldOrder()
    {
        const string signatureInput =
            "merchant_id=10000100&payment_status=COMPLETE&item_name=Emergency+Food+Parcel";
        var signature = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(signatureInput)))
            .ToLowerInvariant();
        var payload = new FormCollection(new Dictionary<string, StringValues>
        {
            ["merchant_id"] = "10000100",
            ["payment_status"] = "COMPLETE",
            ["item_name"] = "Emergency Food Parcel",
            ["signature"] = signature
        });
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var service = new PayFastPaymentService(
            Options.Create(new PayFastOptions { Sandbox = true }),
            httpClient);

        var result = await service.VerifyNotificationAsync(payload);

        Assert.True(result);
        Assert.Equal(signatureInput, handler.PostedBody);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public string? PostedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            PostedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("VALID")
            };
        }
    }
}