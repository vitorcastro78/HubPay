using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using HubPay.Infrastructure.Payments.Webhooks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class BizumStrategy : PaymentStrategyBase
{
    private readonly BizumApiSettings _settings;
    private readonly PspApiClient _api;

    public BizumStrategy(
        HttpClient httpClient,
        ILogger<BizumStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options) : base(httpClient, logger, repository)
    {
        _settings = options.Value.Bizum;
        _api = new PspApiClient(httpClient, _settings, SchemeName, logger);
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public override string SchemeName => "BIZUM";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        PspStrategyHelper.EnsureProductionReady(_settings, SchemeName);
        var phone = PspPhoneValidator.RequirePhone(transaction.CustomerPhone, SchemeName);

        var payload = new
        {
            orderId = transaction.EndToEndId,
            amount = new { value = transaction.Amount, currency = "EUR" },
            buyerPhone = phone,
            notificationUrl = PspStrategyHelper.WebhookUrl(_settings, SchemeName)
        };

        try
        {
            var response = await _api.PostAsync(_settings.PaymentInitPath, payload, ct);
            var externalRef = PspStrategyHelper.ReadString(response, "paymentId", "id") ?? transaction.Id.ToString();
            var redirectUrl = PspStrategyHelper.ReadString(response, "redirectUrl", "confirmationUrl");

            Logger.LogInformation("Bizum iniciado ref={Ref} phone={Phone}", externalRef, phone);
            return new PaymentResult(true, externalRef, "Pending", redirectUrl, JsonSerializer.Serialize(payload));
        }
        catch (PspIntegrationException ex) when (_settings.EnableSimulationFallback)
        {
            return PspStrategyHelper.BuildFallback(
                transaction, SchemeName, "Pending", "https://bizum.es/app/confirm", payload, null, Logger, ex);
        }
    }

    public override Task<WebhookResult> HandleWebhookAsync(
        string payload, Dictionary<string, string> headers, CancellationToken ct) =>
        PspWebhookProcessor.ProcessAsync(
            SchemeName, payload, Repository, Logger,
            root => PspJson.ReadString(root, "paymentId", "id"), ct);

    public override async Task<RefundResult> RefundAsync(Guid transactionId, decimal amount, CancellationToken ct)
    {
        var transaction = await Repository.GetByIdAsync(transactionId, ct);
        if (transaction is null)
            return new RefundResult(false, string.Empty, amount, "NOT_FOUND");

        if (string.IsNullOrWhiteSpace(transaction.ExternalReference))
            return await base.RefundAsync(transactionId, amount, ct);

        var path = _settings.RefundPath.Replace("{paymentId}", transaction.ExternalReference, StringComparison.Ordinal);
        try
        {
            var response = await _api.PostAsync(path, new { amount }, ct);
            var refundId = PspStrategyHelper.ReadString(response, "refundId", "id") ?? $"RF-BZM-{Guid.NewGuid():N}"[..24];
            return new RefundResult(true, refundId, amount, "REFUNDED");
        }
        catch (PspIntegrationException ex) when (_settings.EnableSimulationFallback)
        {
            Logger.LogWarning(ex, "Reembolso Bizum em fallback simulado");
            return await base.RefundAsync(transactionId, amount, ct);
        }
    }
}
