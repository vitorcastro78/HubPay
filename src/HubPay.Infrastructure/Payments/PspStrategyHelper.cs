using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
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
        PaymentSchemeDetails? details,
        ILogger logger,
        Exception ex)
    {
        logger.LogWarning(ex, "PSP {Scheme} em fallback simulado (apenas desenvolvimento)", scheme);
        return new PaymentResult(
            true,
            $"{scheme[..Math.Min(3, scheme.Length)]}-{transaction.Id:N}"[..20],
            status,
            redirectUrl,
            JsonSerializer.Serialize(payload),
            details);
    }

    public static void EnsureProductionReady(PspEndpointSettings settings, string scheme)
    {
        if (settings.EnableSimulationFallback)
            return;

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            throw new PspIntegrationException(scheme, $"BaseUrl do PSP {scheme} não configurada.");

        if (string.IsNullOrWhiteSpace(settings.ApiKey) && string.IsNullOrWhiteSpace(settings.ClientId))
            throw new PspIntegrationException(scheme, $"Credenciais PSP {scheme} (ApiKey ou ClientId) em falta.");
    }

    public static string? ReadString(JsonElement element, params string[] paths) => PspJson.ReadString(element, paths);
}
