using System.Security.Cryptography;
using System.Text;
using HubPay.Domain.Interfaces;

namespace HubPay.Infrastructure.Webhooks;

public sealed class HmacWebhookSignatureValidator : IWebhookSignatureValidator
{
    private readonly IHubPaySettingsProvider _settingsProvider;

    public HmacWebhookSignatureValidator(IHubPaySettingsProvider settingsProvider) =>
        _settingsProvider = settingsProvider;

    public bool Validate(string scheme, string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

        var webhooks = _settingsProvider.Current.Webhooks;
        var secret = ResolveSecret(webhooks.SchemeSecrets, scheme);
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

    private static string ResolveSecret(Dictionary<string, string> secrets, string scheme)
    {
        if (secrets.TryGetValue(scheme.ToUpperInvariant(), out var secret) && !string.IsNullOrEmpty(secret))
            return secret;
        return secrets.GetValueOrDefault("DEFAULT") ?? string.Empty;
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
