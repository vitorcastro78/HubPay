using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HubPay.Application.DTOs;
using HubPay.Application.DTOs.Admin;
using HubPay.Application.Queries;

namespace HubPay.Frontend.Blazor.Services;

public sealed class HubPayApiClient
{
    private readonly HttpClient _http;
    private string? _accessToken;

    public HubPayApiClient(HttpClient http) => _http = http;

    public async Task<string?> AuthenticateAsync(string merchantId, string role = "merchant")
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/token", new { merchantId, role });
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

    public async Task<IReadOnlyList<PspProviderConfigDto>?> GetPspProvidersAsync()
    {
        await EnsureAdminAsync();
        return await _http.GetFromJsonAsync<IReadOnlyList<PspProviderConfigDto>>("api/v1/admin/psp-config/providers");
    }

    public async Task<PspProviderConfigDto?> GetPspProviderAsync(string scheme)
    {
        await EnsureAdminAsync();
        var response = await _http.GetAsync($"api/v1/admin/psp-config/providers/{scheme}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PspProviderConfigDto>()
            : null;
    }

    public async Task<PspProviderConfigDto?> CreatePspProviderAsync(CreatePspProviderRequest request)
    {
        await EnsureAdminAsync();
        var response = await _http.PostAsJsonAsync("api/v1/admin/psp-config/providers", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PspProviderConfigDto>()
            : null;
    }

    public async Task<PspProviderConfigDto?> UpdatePspProviderAsync(string scheme, UpdatePspProviderRequest request)
    {
        await EnsureAdminAsync();
        var response = await _http.PutAsJsonAsync($"api/v1/admin/psp-config/providers/{scheme}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PspProviderConfigDto>()
            : null;
    }

    public async Task<bool> DeletePspProviderAsync(string scheme)
    {
        await EnsureAdminAsync();
        var response = await _http.DeleteAsync($"api/v1/admin/psp-config/providers/{scheme}");
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<PspMerchantConfigDto>?> GetPspMerchantsAsync(string? scheme = null)
    {
        await EnsureAdminAsync();
        var url = string.IsNullOrWhiteSpace(scheme)
            ? "api/v1/admin/psp-config/merchants"
            : $"api/v1/admin/psp-config/merchants?scheme={Uri.EscapeDataString(scheme)}";
        return await _http.GetFromJsonAsync<IReadOnlyList<PspMerchantConfigDto>>(url);
    }

    public async Task<PspMerchantConfigDto?> UpsertPspMerchantAsync(UpsertPspMerchantRequest request, bool isUpdate)
    {
        await EnsureAdminAsync();
        var response = isUpdate
            ? await _http.PutAsJsonAsync($"api/v1/admin/psp-config/merchants/{request.Scheme}/{request.MerchantId}", request)
            : await _http.PostAsJsonAsync("api/v1/admin/psp-config/merchants", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PspMerchantConfigDto>()
            : null;
    }

    public async Task<bool> DeletePspMerchantAsync(string scheme, string merchantId)
    {
        await EnsureAdminAsync();
        var response = await _http.DeleteAsync($"api/v1/admin/psp-config/merchants/{scheme}/{merchantId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<PspConfigurationReloadResult?> ReloadPspConfigurationAsync()
    {
        await EnsureAdminAsync();
        var response = await _http.PostAsync("api/v1/admin/psp-config/reload", null);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PspConfigurationReloadResult>()
            : null;
    }

    public async Task<string?> GetErrorBodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync();

    private async Task<T?> SendAuthorizedAsync<T>(string url)
    {
        await EnsureAuthenticatedAsync();
        return await _http.GetFromJsonAsync<T>(url);
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_accessToken is null)
            await AuthenticateAsync("DEMO-MERCHANT-001", "merchant");
    }

    private async Task EnsureAdminAsync()
    {
        if (_accessToken is null)
            await AuthenticateAsync("HUBPAY-ADMIN", "admin");
    }
}
