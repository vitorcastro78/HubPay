using HubPay.Application.DTOs.Admin;

namespace HubPay.Application.Interfaces;

public interface IPspConfigurationAdminService
{
    Task<IReadOnlyList<PspProviderConfigDto>> ListProvidersAsync(CancellationToken ct = default);
    Task<PspProviderConfigDto?> GetProviderAsync(string scheme, CancellationToken ct = default);
    Task<PspProviderConfigDto> CreateProviderAsync(CreatePspProviderRequest request, CancellationToken ct = default);
    Task<PspProviderConfigDto> UpdateProviderAsync(string scheme, UpdatePspProviderRequest request, CancellationToken ct = default);
    Task DeleteProviderAsync(string scheme, CancellationToken ct = default);
    Task<IReadOnlyList<PspMerchantConfigDto>> ListMerchantsAsync(string? scheme = null, CancellationToken ct = default);
    Task<PspMerchantConfigDto?> GetMerchantAsync(string scheme, string merchantId, CancellationToken ct = default);
    Task<PspMerchantConfigDto> UpsertMerchantAsync(UpsertPspMerchantRequest request, CancellationToken ct = default);
    Task DeleteMerchantAsync(string scheme, string merchantId, CancellationToken ct = default);
    Task<PspConfigurationReloadResult> ReloadAsync(CancellationToken ct = default);
}
