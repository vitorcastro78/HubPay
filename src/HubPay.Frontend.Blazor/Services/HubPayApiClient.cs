using System.Net.Http.Json;
using HubPay.Application.DTOs;
using HubPay.Application.Queries;

namespace HubPay.Frontend.Blazor.Services;

public sealed class HubPayApiClient
{
    private readonly HttpClient _http;

    public HubPayApiClient(HttpClient http) => _http = http;

    public async Task<DashboardStatsDto?> GetDashboardStatsAsync() =>
        await _http.GetFromJsonAsync<DashboardStatsDto>("api/v1/dashboard/stats");

    public async Task<PagedResult<TransactionDto>?> GetTransactionsAsync(int page = 1, int pageSize = 20) =>
        await _http.GetFromJsonAsync<PagedResult<TransactionDto>>($"api/v1/transactions?page={page}&pageSize={pageSize}");

    public async Task<AntiFraudDetailDto?> GetAntiFraudDetailAsync(Guid id) =>
        await _http.GetFromJsonAsync<AntiFraudDetailDto>($"api/v1/transactions/{id}/antifraud");

    public async Task<bool> RefundAsync(Guid id, decimal? amount = null)
    {
        var url = amount.HasValue
            ? $"api/v1/transactions/{id}/refund?amount={amount.Value}"
            : $"api/v1/transactions/{id}/refund";
        var response = await _http.PostAsync(url, null);
        return response.IsSuccessStatusCode;
    }
}
