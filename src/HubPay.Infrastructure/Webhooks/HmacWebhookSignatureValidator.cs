using System.Security.Cryptography;
using System.Text;
using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Webhooks;

public sealed class HmacWebhookSignatureValidator : IWebhookSignatureValidator
{
    private readonly WebhookSettings _settings;

    public HmacWebhookSignatureValidator(IOptions<HubPaySettings> options) =>
        _settings = options.Value.Webhooks;

    public bool Validate(string scheme, string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

        var secret = ResolveSecret(scheme);
        var expected = ComputeHmacSha256(payload, secret);
        var provided = signatureHeader.Trim();

        if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            provided = provided["sha256=".Length..];

        try
        {
            var expectedBytes = Convert.FromHexString(expected);
            var providedBytes = Convert.FromHexString(provided);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
        catch (FormatException)
        {
            return string.Equals(expected, provided, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string ResolveSecret(string scheme)
    {
        if (_settings.SchemeSecrets.TryGetValue(scheme.ToUpperInvariant(), out var secret))
            return secret;
        return _settings.SchemeSecrets["DEFAULT"];
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
