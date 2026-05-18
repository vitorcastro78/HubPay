using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments;

internal static class PspStrategyHelper
{
    public static string WebhookUrl(PspEndpointSettings settings, string scheme) =>
        $"{settings.WebhookBaseUrl.TrimEnd('/')}/api/v1/webhooks/{scheme.ToLowerInvariant()}";

    public static PaymentResult BuildFallback(
        Transaction transaction,
        string scheme,
        string status,
        string? redirectUrl,
        object payload,
        ILogger logger,
        Exception ex)
    {
        logger.LogWarning(ex, "PSP {Scheme} em fallback simulado", scheme);
        return new PaymentResult(
            true,
            $"{scheme[..Math.Min(3, scheme.Length)]}-{transaction.Id:N}"[..20],
            status,
            redirectUrl,
            JsonSerializer.Serialize(payload));
    }

    public static string? ReadString(JsonElement element, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (element.TryGetProperty(path, out var prop))
            {
                var value = prop.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        return null;
    }
}
