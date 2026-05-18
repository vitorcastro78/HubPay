using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HubPay.WebApi.Hubs;

[Authorize]
public sealed class TransactionHub : Hub
{
    public const string HubPath = "/hubs/transactions";

    public async Task SubscribeToMerchant(string merchantId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, MerchantGroup(merchantId));

    public static string MerchantGroup(string merchantId) => $"merchant:{merchantId}";
}
