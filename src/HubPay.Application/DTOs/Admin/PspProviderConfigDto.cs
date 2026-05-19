namespace HubPay.Application.DTOs.Admin;

/// <summary>PSP provider configuration row stored in PostgreSQL.</summary>
/// <param name="Id">Surrogate key (GUID).</param>
/// <param name="Scheme">Logical provider key: SIBS, BIZUM, WERO, WEBHOOKS, etc.</param>
/// <param name="IsEnabled">When false, loader skips this provider.</param>
/// <param name="SettingsJson">Provider-specific JSON settings (camelCase).</param>
/// <param name="UpdatedAt">Last update timestamp (UTC).</param>
public sealed record PspProviderConfigDto(
    Guid Id,
    string Scheme,
    bool IsEnabled,
    string SettingsJson,
    DateTime UpdatedAt);

/// <summary>Request to create a PSP provider configuration.</summary>
public sealed record CreatePspProviderRequest(
    string Scheme,
    bool IsEnabled,
    string SettingsJson);

/// <summary>Request to update an existing PSP provider.</summary>
/// <param name="IsEnabled">Optional enable flag.</param>
/// <param name="SettingsJson">Replacement JSON settings.</param>
public sealed record UpdatePspProviderRequest(
    bool? IsEnabled,
    string SettingsJson);

/// <summary>Per-merchant override for a PSP provider.</summary>
public sealed record PspMerchantConfigDto(
    Guid Id,
    string Scheme,
    string MerchantId,
    string SettingsJson,
    DateTime UpdatedAt);

/// <summary>Create or replace merchant-specific PSP settings.</summary>
public sealed record UpsertPspMerchantRequest(
    string Scheme,
    string MerchantId,
    string SettingsJson);

/// <summary>Result of hot-reloading PSP configuration from the database.</summary>
public sealed record PspConfigurationReloadResult(
    bool Success,
    int ProviderCount,
    int MerchantCount,
    DateTime ReloadedAt,
    string Message);
