using System.Text.Json;
using HubPay.Application.DTOs.Admin;
using HubPay.Application.Interfaces;
using HubPay.Domain.Entities;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.Payments.MutualTls;
using HubPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Configuration;

public sealed class PspConfigurationAdminService : IPspConfigurationAdminService
{
    private readonly HubPayDbContext _db;
    private readonly DatabaseHubPaySettingsLoader _loader;
    private readonly IHubPaySettingsProvider _settingsProvider;
    private readonly ILogger<PspConfigurationAdminService> _logger;

    public PspConfigurationAdminService(
        HubPayDbContext db,
        DatabaseHubPaySettingsLoader loader,
        IHubPaySettingsProvider settingsProvider,
        ILogger<PspConfigurationAdminService> logger)
    {
        _db = db;
        _loader = loader;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PspProviderConfigDto>> ListProvidersAsync(CancellationToken ct = default) =>
        await _db.PspProviderConfigurations
            .AsNoTracking()
            .OrderBy(p => p.Scheme)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

    public async Task<PspProviderConfigDto?> GetProviderAsync(string scheme, CancellationToken ct = default)
    {
        var row = await FindProviderAsync(scheme, ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<PspProviderConfigDto> CreateProviderAsync(CreatePspProviderRequest request, CancellationToken ct = default)
    {
        ValidateScheme(request.Scheme);
        ValidateJson(request.SettingsJson);

        if (await _db.PspProviderConfigurations.AnyAsync(p => p.Scheme == request.Scheme.ToUpperInvariant(), ct))
            throw new BusinessRuleException($"O esquema {request.Scheme} já existe.");

        var entity = new PspProviderConfiguration
        {
            Id = Guid.NewGuid(),
            Scheme = request.Scheme.ToUpperInvariant(),
            IsEnabled = request.IsEnabled,
            SettingsJson = request.SettingsJson,
            UpdatedAt = DateTime.UtcNow
        };

        _db.PspProviderConfigurations.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PSP provider {Scheme} criado.", entity.Scheme);
        return ToDto(entity);
    }

    public async Task<PspProviderConfigDto> UpdateProviderAsync(
        string scheme,
        UpdatePspProviderRequest request,
        CancellationToken ct = default)
    {
        ValidateJson(request.SettingsJson);
        var entity = await FindProviderAsync(scheme, ct)
                       ?? throw new BusinessRuleException($"Esquema {scheme} não encontrado.");

        entity.SettingsJson = request.SettingsJson;
        if (request.IsEnabled.HasValue)
            entity.IsEnabled = request.IsEnabled.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PSP provider {Scheme} atualizado.", entity.Scheme);
        return ToDto(entity);
    }

    public async Task DeleteProviderAsync(string scheme, CancellationToken ct = default)
    {
        var entity = await FindProviderAsync(scheme, ct)
                     ?? throw new BusinessRuleException($"Esquema {scheme} não encontrado.");
        _db.PspProviderConfigurations.Remove(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PSP provider {Scheme} removido.", entity.Scheme);
    }

    public async Task<IReadOnlyList<PspMerchantConfigDto>> ListMerchantsAsync(string? scheme = null, CancellationToken ct = default)
    {
        var query = _db.PspMerchantConfigurations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(scheme))
            query = query.Where(m => m.Scheme == scheme.ToUpperInvariant());

        return await query
            .OrderBy(m => m.Scheme)
            .ThenBy(m => m.MerchantId)
            .Select(m => ToDto(m))
            .ToListAsync(ct);
    }

    public async Task<PspMerchantConfigDto?> GetMerchantAsync(string scheme, string merchantId, CancellationToken ct = default)
    {
        var row = await FindMerchantAsync(scheme, merchantId, ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<PspMerchantConfigDto> UpsertMerchantAsync(UpsertPspMerchantRequest request, CancellationToken ct = default)
    {
        ValidateScheme(request.Scheme);
        ValidateJson(request.SettingsJson);

        if (string.IsNullOrWhiteSpace(request.MerchantId))
            throw new BusinessRuleException("MerchantId é obrigatório.");

        var scheme = request.Scheme.ToUpperInvariant();
        var entity = await FindMerchantAsync(scheme, request.MerchantId, ct);

        if (entity is null)
        {
            entity = new PspMerchantConfiguration
            {
                Id = Guid.NewGuid(),
                Scheme = scheme,
                MerchantId = request.MerchantId,
                SettingsJson = request.SettingsJson,
                UpdatedAt = DateTime.UtcNow
            };
            _db.PspMerchantConfigurations.Add(entity);
        }
        else
        {
            entity.SettingsJson = request.SettingsJson;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PSP merchant {Scheme}/{Merchant} gravado.", scheme, request.MerchantId);
        return ToDto(entity);
    }

    public async Task DeleteMerchantAsync(string scheme, string merchantId, CancellationToken ct = default)
    {
        var entity = await FindMerchantAsync(scheme, merchantId, ct)
                     ?? throw new BusinessRuleException("Configuração merchant não encontrada.");
        _db.PspMerchantConfigurations.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PspConfigurationReloadResult> ReloadAsync(CancellationToken ct = default)
    {
        var settings = await _loader.LoadAsync(ct);
        _settingsProvider.Initialize(settings);

        var providerCount = await _db.PspProviderConfigurations.CountAsync(p => p.IsEnabled, ct);
        var merchantCount = await _db.PspMerchantConfigurations.CountAsync(ct);

        _logger.LogInformation(
            "Configurações PSP recarregadas em memória ({Providers} providers, {Merchants} merchants).",
            providerCount, merchantCount);

        return new PspConfigurationReloadResult(
            true,
            providerCount,
            merchantCount,
            DateTime.UtcNow,
            "Configurações PSP recarregadas sem reiniciar a aplicação.");
    }

    private async Task<PspProviderConfiguration?> FindProviderAsync(string scheme, CancellationToken ct) =>
        await _db.PspProviderConfigurations
            .FirstOrDefaultAsync(p => p.Scheme == scheme.ToUpperInvariant(), ct);

    private async Task<PspMerchantConfiguration?> FindMerchantAsync(string scheme, string merchantId, CancellationToken ct) =>
        await _db.PspMerchantConfigurations
            .FirstOrDefaultAsync(
                m => m.Scheme == scheme.ToUpperInvariant() && m.MerchantId == merchantId,
                ct);

    private static PspProviderConfigDto ToDto(PspProviderConfiguration p) =>
        new(p.Id, p.Scheme, p.IsEnabled, p.SettingsJson, p.UpdatedAt);

    private static PspMerchantConfigDto ToDto(PspMerchantConfiguration m) =>
        new(m.Id, m.Scheme, m.MerchantId, m.SettingsJson, m.UpdatedAt);

    private static void ValidateScheme(string scheme)
    {
        if (string.IsNullOrWhiteSpace(scheme))
            throw new BusinessRuleException("Scheme é obrigatório.");
    }

    private static void ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new BusinessRuleException("SettingsJson é obrigatório.");

        try
        {
            JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"JSON inválido: {ex.Message}");
        }
    }
}
