using System.Net.Http.Json;
using System.Text.Json;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments;

public abstract class PaymentStrategyBase : IPaymentStrategy
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly ITransactionRepository Repository;

    protected PaymentStrategyBase(HttpClient httpClient, ILogger logger, ITransactionRepository repository)
    {
        HttpClient = httpClient;
        Logger = logger;
        Repository = repository;
    }

    public abstract string SchemeName { get; }

    public abstract Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct);

    public virtual async Task<WebhookResult> HandleWebhookAsync(string payload, Dictionary<string, string> headers, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var externalId = root.TryGetProperty("transactionId", out var tid)
                ? tid.GetString()
                : root.TryGetProperty("id", out var id) ? id.GetString() : null;

            if (externalId is null || !Guid.TryParse(externalId, out var txId))
                return new WebhookResult(false, null, "IGNORED", payload);

            var transaction = await Repository.GetByIdAsync(txId, ct);
            if (transaction is null)
                return new WebhookResult(false, null, "NOT_FOUND", payload);

            var status = root.TryGetProperty("status", out var st) ? st.GetString() : "AUTHORIZED";
            if (string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "AUTHORIZED", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Authorize();
                await Repository.UpdateAsync(transaction, ct);
                return new WebhookResult(true, txId, "Authorized", payload);
            }

            return new WebhookResult(true, txId, status ?? "UNKNOWN", payload);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao processar webhook {Scheme}", SchemeName);
            return new WebhookResult(false, null, "ERROR", payload);
        }
    }

    public virtual async Task<RefundResult> RefundAsync(Guid transactionId, decimal amount, CancellationToken ct)
    {
        var transaction = await Repository.GetByIdAsync(transactionId, ct);
        if (transaction is null)
            return new RefundResult(false, string.Empty, amount, "NOT_FOUND");

        var refundRef = $"RF-{SchemeName}-{Guid.NewGuid():N}"[..24];
        Logger.LogInformation("Reembolso {Scheme} tx={TxId} amount={Amount}", SchemeName, transactionId, amount);
        return new RefundResult(true, refundRef, amount, "REFUNDED");
    }

    protected async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct)
    {
        var response = await HttpClient.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    }
}
