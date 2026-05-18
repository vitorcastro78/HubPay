using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.Webhooks;

public static class PspWebhookProcessor
{
    private static readonly HashSet<string> SuccessStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUCCESS", "SUCCEEDED", "PAID", "AUTHORIZED", "ACCEPTED", "COMPLETED", "SETTLED", "CAPTURED"
    };

    private static readonly HashSet<string> FailureStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "FAILED", "DECLINED", "CANCELLED", "CANCELED", "EXPIRED", "REJECTED", "ERROR"
    };

    public static async Task<WebhookResult> ProcessAsync(
        string scheme,
        string payload,
        ITransactionRepository repository,
        ILogger logger,
        Func<JsonElement, string?> externalIdSelector,
        CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var transaction = await ResolveTransactionAsync(root, externalIdSelector, repository, ct);
            if (transaction is null)
                return new WebhookResult(false, null, "NOT_FOUND", payload);

            var status = ReadStatus(root);
            if (SuccessStatuses.Contains(status))
            {
                var externalRef = externalIdSelector(root) ?? transaction.ExternalReference;
                transaction.Authorize(externalRef);
                await repository.UpdateAsync(transaction, ct);
                logger.LogInformation("Webhook {Scheme} autorizou tx={TxId}", scheme, transaction.Id);
                return new WebhookResult(true, transaction.Id, "Authorized", payload);
            }

            if (FailureStatuses.Contains(status))
            {
                transaction.Fail(status);
                await repository.UpdateAsync(transaction, ct);
                return new WebhookResult(true, transaction.Id, status, payload);
            }

            return new WebhookResult(true, transaction.Id, status, payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar webhook {Scheme}", scheme);
            return new WebhookResult(false, null, "ERROR", payload);
        }
    }

    public static async Task<Transaction?> ResolveTransactionAsync(
        JsonElement root,
        Func<JsonElement, string?> externalIdSelector,
        ITransactionRepository repository,
        CancellationToken ct)
    {
        if (TryGetGuid(root, "merchantTransactionId", out var merchantTxId) ||
            TryGetGuid(root, "transactionId", out merchantTxId) ||
            TryGetGuid(root, "merchantReference", out merchantTxId))
        {
            var byId = await repository.GetByIdAsync(merchantTxId, ct);
            if (byId is not null)
                return byId;
        }

        var externalId = externalIdSelector(root);
        if (!string.IsNullOrWhiteSpace(externalId))
        {
            var byExternal = await repository.GetByExternalReferenceAsync(externalId, ct);
            if (byExternal is not null)
                return byExternal;
        }

        var orderId = PspJson.ReadString(root, "orderId", "endToEndId", "remittanceInformation");
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            var byE2E = await repository.GetByEndToEndIdAsync(orderId, ct);
            if (byE2E is not null)
                return byE2E;
        }

        return null;
    }

    private static string ReadStatus(JsonElement root) =>
        PspJson.ReadString(root, "status", "paymentStatus", "transactionStatus", "state") ?? "UNKNOWN";

    private static bool TryGetGuid(JsonElement root, string property, out Guid id)
    {
        id = default;
        if (!root.TryGetProperty(property, out var prop))
            return false;
        var value = prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText();
        return value is not null && Guid.TryParse(value, out id);
    }
}
