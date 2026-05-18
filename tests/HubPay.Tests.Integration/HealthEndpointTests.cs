using System.Net;
using System.Net.Http.Json;

namespace HubPay.Tests.Integration;

public sealed class HealthEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOkOrDegradedWhenInfraOffline()
    {
        var response = await _client.GetAsync("/health");
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Health respondeu {response.StatusCode}");
    }

    [Fact]
    public async Task AuthToken_WithMerchant_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/token", new { merchantId = "TEST-MERCHANT" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
