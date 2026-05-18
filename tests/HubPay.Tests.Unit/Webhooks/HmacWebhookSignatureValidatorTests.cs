using HubPay.Domain.Configuration;
using HubPay.Infrastructure.Configuration;
using HubPay.Infrastructure.Webhooks;

namespace HubPay.Tests.Unit.Webhooks;

public sealed class HmacWebhookSignatureValidatorTests
{
    [Fact]
    public void Validate_ValidSignature_ReturnsTrue()
    {
        var provider = new HubPaySettingsProvider();
        provider.Initialize(new HubPaySettings
        {
            Webhooks = new WebhookSettings
            {
                SchemeSecrets = new Dictionary<string, string> { ["MBWAY"] = "secret-test" }
            }
        });

        var validator = new HmacWebhookSignatureValidator(provider);
        var payload = """{"status":"SUCCESS"}""";
        var secret = "secret-test";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        Assert.True(validator.Validate("MBWAY", payload, $"sha256={hash}"));
    }

    [Fact]
    public void Validate_InvalidSignature_ReturnsFalse()
    {
        var provider = new HubPaySettingsProvider();
        provider.Initialize(new HubPaySettings());
        var validator = new HmacWebhookSignatureValidator(provider);
        Assert.False(validator.Validate("MBWAY", "{}", "invalid"));
    }
}
