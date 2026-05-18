namespace HubPay.Application.DTOs.Admin;

public sealed record PspProviderConfigDto(
    Guid Id,
    string Scheme,
    bool IsEnabled,
    string SettingsJson,
    DateTime UpdatedAt);

public sealed record CreatePspProviderRequest(
    string Scheme,
    bool IsEnabled,
    string SettingsJson);

public sealed record UpdatePspProviderRequest(
    bool? IsEnabled,
    string SettingsJson);

public sealed record PspMerchantConfigDto(
    Guid Id,
    string Scheme,
    string MerchantId,
    string SettingsJson,
    DateTime UpdatedAt);

public sealed record UpsertPspMerchantRequest(
    string Scheme,
    string MerchantId,
    string SettingsJson);

public sealed record PspConfigurationReloadResult(
    bool Success,
    int ProviderCount,
    int MerchantCount,
    DateTime ReloadedAt,
    string Message);
