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

public sealed class CartesBancairesStrategy : PaymentStrategyBase
{
    private readonly CartesBancairesApiSettings _settings;
    private readonly PspApiClient _api;

    public CartesBancairesStrategy(
        HttpClient httpClient,
        ILogger<CartesBancairesStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options) : base(httpClient, logger, repository)
    {
        _settings = options.Value.CartesBancaires;
        _api = new PspApiClient(httpClient, _settings, SchemeName, logger);
    }

    public override string SchemeName => "CARTESBANCAIRES";

    public override async Task<PaymentResult> ProcessAsync(Transaction transaction, CancellationToken ct)
    {
        PspStrategyHelper.EnsureProductionReady(_settings, SchemeName);

        var payload = new
        {
            scheme = "CB",
            amount = new { value = transaction.Amount, currency = "EUR" },
            merchantId = transaction.MerchantId,
            merchantTransactionId = transaction.Id.ToString(),
            authentication = "3DS2",
            notificationUrl = PspStrategyHelper.WebhookUrl(_settings, SchemeName)
        };

        try
        {
            var response = await _api.PostAsync(_settings.AuthorizePath, payload, ct);
            var externalRef = PspStrategyHelper.ReadString(response, "paymentId", "id") ?? transaction.Id.ToString();
            var acsUrl = PspStrategyHelper.ReadString(response, "acsUrl", "threeDsChallengeUrl", "redirectUrl")
                         ?? "https://acs.cartesbancaires.fr/challenge";
            var sessionId = PspStrategyHelper.ReadString(response, "threeDsSessionId", "sessionId");

            var threeDs = new
            {
                type = "3DS_CHALLENGE",
                acsUrl,
                sessionId,
                paymentId = externalRef,
                transactionId = transaction.Id
            };

            var details = new PaymentSchemeDetails(ThreeDsChallenge: threeDs);
            Logger.LogInformation("Cartes Bancaires 3DS iniciado ref={Ref}", externalRef);
            return new PaymentResult(true, externalRef, "Pending", acsUrl, JsonSerializer.Serialize(payload), details);
        }
        catch (PspIntegrationException ex) when (_settings.EnableSimulationFallback)
        {
            var fallback3Ds = new { type = "3DS_CHALLENGE", acsUrl = "https://acs.cartesbancaires.fr/challenge", transactionId = transaction.Id };
            return PspStrategyHelper.BuildFallback(
                transaction, SchemeName, "Pending", "https://acs.cartesbancaires.fr/challenge", payload,
                new PaymentSchemeDetails(ThreeDsChallenge: fallback3Ds), Logger, ex);
        }
    }

    public override async Task<WebhookResult> HandleWebhookAsync(
        string payload, Dictionary<string, string> headers, CancellationToken ct)
    {
        var result = await PspWebhookProcessor.ProcessAsync(
            SchemeName, payload, Repository, Logger,
            root => PspJson.ReadString(root, "paymentId", "id"), ct);

        if (result.TransactionId is null || result.NewStatus is "NOT_FOUND" or "ERROR" or "IGNORED")
            return result;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var statusPath = _settings.ThreeDsStatusPath.Replace(
                "{paymentId}",
                PspJson.ReadString(doc.RootElement, "paymentId", "id") ?? string.Empty,
                StringComparison.Ordinal);
            if (!string.IsNullOrWhiteSpace(statusPath) && statusPath.Contains('{') == false)
                await _api.GetAsync(statusPath, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Consulta 3DS status Cartes Bancaires falhou após webhook");
        }

        return result;
    }
}
