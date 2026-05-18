using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HubPay.Application.DTOs;
using HubPay.Application.Queries;

namespace HubPay.Frontend.Blazor.Services;

public sealed class HubPayApiClient
{
    private readonly HttpClient _http;
    private string? _accessToken;

    public HubPayApiClient(HttpClient http) => _http = http;

    public async Task<string?> AuthenticateAsync(string merchantId)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/token", new { merchantId, role = "merchant" });
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (json.TryGetProperty("accessToken", out var token))
        {
            _accessToken = token.GetString();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return _accessToken;
        }
        return null;
    }

    public async Task<DashboardStatsDto?> GetDashboardStatsAsync() =>
        await SendAuthorizedAsync<DashboardStatsDto>("api/v1/dashboard/stats");

    public async Task<PagedResult<TransactionDto>?> GetTransactionsAsync(int page = 1, int pageSize = 20) =>
        await SendAuthorizedAsync<PagedResult<TransactionDto>>($"api/v1/transactions?page={page}&pageSize={pageSize}");

    public async Task<AntiFraudDetailDto?> GetAntiFraudDetailAsync(Guid id) =>
        await SendAuthorizedAsync<AntiFraudDetailDto>($"api/v1/transactions/{id}/antifraud");

    public async Task<bool> RefundAsync(Guid id, decimal? amount = null)
    {
        await EnsureAuthenticatedAsync();
        var url = amount.HasValue
            ? $"api/v1/transactions/{id}/refund?amount={amount.Value}"
            : $"api/v1/transactions/{id}/refund";
        var response = await _http.PostAsync(url, null);
        return response.IsSuccessStatusCode;
    }

    private async Task<T?> SendAuthorizedAsync<T>(string url)
    {
        await EnsureAuthenticatedAsync();
        return await _http.GetFromJsonAsync<T>(url);
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_accessToken is null)
            await AuthenticateAsync("DEMO-MERCHANT-001");
    }
}
