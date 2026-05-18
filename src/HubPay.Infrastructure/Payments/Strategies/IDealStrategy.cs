using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.Strategies;

public sealed class IDealStrategy : PspEndpointStrategyBase
{
    public IDealStrategy(
        HttpClient httpClient,
        ILogger<IDealStrategy> logger,
        ITransactionRepository repository,
        IOptions<HubPaySettings> options)
        : base(httpClient, options.Value.Ideal, "IDEAL", logger, repository) { }

    public override string SchemeName => "IDEAL";
    protected override string PaymentInitPath => "/v1/transactions";
    protected override string? DefaultRedirectUrl => "https://ideal.nl/checkout";

    protected override object BuildPaymentPayload(Transaction transaction)
    {
        var token = Guid.NewGuid().ToString("N");
        return new
        {
            transactionToken = token,
            amount = new { value = transaction.Amount, currency = "EUR" },
            endToEndId = transaction.EndToEndId,
            returnUrl = PspStrategyHelper.WebhookUrl(Settings, SchemeName)
        };
    }

    protected override PaymentResult MapPaymentResult(Transaction transaction, JsonElement response, object payload)
    {
        var token = PspStrategyHelper.ReadString(response, "transactionToken", "token")
                    ?? Guid.NewGuid().ToString("N");
        var redirectUrl = PspStrategyHelper.ReadString(response, "redirectUrl", "issuerAuthenticationUrl")
                          ?? $"{DefaultRedirectUrl}?token={token}";
        var qrPayload = PspStrategyHelper.ReadString(response, "qrCode", "qrPayload")
                        ?? IdealQrBuilder.Build(token, transaction.Amount);
        var emvQr = IdealQrBuilder.BuildEmvQr(token, transaction.Amount, transaction.MerchantId);
        var externalRef = PspStrategyHelper.ReadString(response, "paymentId", "id", "transactionId")
                          ?? $"IDL-{token}"[..24];

        var details = new PaymentSchemeDetails(IdealQrPayload: qrPayload);
        Logger.LogInformation("iDEAL iniciado token={Token}", token);

        var enrichedPayload = new
        {
            transactionToken = token,
            redirectUrl,
            qrCode = qrPayload,
            emvQr,
            endToEndId = transaction.EndToEndId
        };

        return new PaymentResult(
            true, externalRef, "Pending", redirectUrl,
            JsonSerializer.Serialize(enrichedPayload), details);
    }
}
