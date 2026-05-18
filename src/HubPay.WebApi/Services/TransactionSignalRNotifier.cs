using HubPay.Application.DTOs;
using HubPay.Domain.Entities;
using HubPay.Domain.Interfaces;
using HubPay.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HubPay.WebApi.Services;

public sealed class TransactionSignalRNotifier : ITransactionNotifier
{
    private readonly IHubContext<TransactionHub> _hub;

    public TransactionSignalRNotifier(IHubContext<TransactionHub> hub) => _hub = hub;

    public async Task NotifyUpdatedAsync(Transaction transaction, CancellationToken ct = default)
    {
        var dto = new TransactionDto(
            transaction.Id,
            transaction.MerchantId,
            transaction.Amount,
            transaction.Currency,
            transaction.PaymentScheme,
            transaction.EndToEndId,
            transaction.Status.ToString(),
            transaction.ScaStatus,
            transaction.AntiFraudScore,
            transaction.CountryCode,
            transaction.CreatedAt,
            transaction.AntiFraudElapsedMs);

        await _hub.Clients.Group(TransactionHub.MerchantGroup(transaction.MerchantId))
            .SendAsync("TransactionUpdated", dto, ct);

        await _hub.Clients.All.SendAsync("TransactionUpdated", dto, ct);
    }
}
