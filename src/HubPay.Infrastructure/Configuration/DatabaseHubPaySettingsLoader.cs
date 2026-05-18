using System.Text.Json;
using System.Text.Json.Serialization;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Configuration;

public sealed class DatabaseHubPaySettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HubPayDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseHubPaySettingsLoader> _logger;

    public DatabaseHubPaySettingsLoader(
        HubPayDbContext db,
        IConfiguration configuration,
        ILogger<DatabaseHubPaySettingsLoader> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HubPaySettings> LoadAsync(CancellationToken ct = default)
    {
        var settings = _configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()
                       ?? new HubPaySettings();

        var providers = await _db.PspProviderConfigurations
            .AsNoTracking()
            .Where(p => p.IsEnabled)
            .ToListAsync(ct);

        if (providers.Count == 0)
        {
            _logger.LogWarning("Nenhuma configuração PSP na base de dados — a usar appsettings.");
            return settings;
        }

        var merchants = await _db.PspMerchantConfigurations.AsNoTracking().ToListAsync(ct);
        ApplyProviders(settings, providers);
        ApplyMerchants(settings, merchants);

        _logger.LogInformation("Configurações PSP carregadas da BD ({Count} provedores, {Merchants} merchants).",
            providers.Count, merchants.Count);

        return settings;
    }

    private static void ApplyProviders(HubPaySettings settings, IReadOnlyList<PspProviderConfiguration> providers)
    {
        foreach (var row in providers)
        {
            switch (row.Scheme.ToUpperInvariant())
            {
                case "SIBS":
                    settings.Sibs = DeserializeOrDefault(row.SettingsJson, settings.Sibs);
                    break;
                case "BIZUM":
                    settings.Bizum = DeserializeOrDefault(row.SettingsJson, settings.Bizum);
                    break;
                case "WERO":
                    settings.Wero = DeserializeOrDefault(row.SettingsJson, settings.Wero);
                    break;
                case "CARTESBANCAIRES":
                    settings.CartesBancaires = DeserializeOrDefault(row.SettingsJson, settings.CartesBancaires);
                    break;
                case "IDEAL":
                    settings.Ideal = DeserializeOrDefault(row.SettingsJson, settings.Ideal);
                    break;
                case "BANCONTACT":
                    settings.Bancontact = DeserializeOrDefault(row.SettingsJson, settings.Bancontact);
                    break;
                case "EURO6000":
                    settings.Euro6000 = DeserializeOrDefault(row.SettingsJson, settings.Euro6000);
                    break;
                case "BANCOMATPAY":
                    settings.BancomatPay = DeserializeOrDefault(row.SettingsJson, settings.BancomatPay);
                    break;
                case "SWISH":
                    settings.Swish = DeserializeOrDefault(row.SettingsJson, settings.Swish);
                    break;
                case "VIPPSMOBILEPAY":
                    settings.VippsMobilePay = DeserializeOrDefault(row.SettingsJson, settings.VippsMobilePay);
                    break;
                case "WEBHOOKS":
                    settings.Webhooks = DeserializeOrDefault(row.SettingsJson, settings.Webhooks);
                    break;
            }
        }
    }

    private static void ApplyMerchants(HubPaySettings settings, IReadOnlyList<PspMerchantConfiguration> merchants)
    {
        foreach (var row in merchants)
        {
            switch (row.Scheme.ToUpperInvariant())
            {
                case "SIBS":
                    ApplySibsMerchant(settings.Sibs, row);
                    break;
                case "WERO":
                    ApplyWeroMerchant(settings.Wero, row);
                    break;
            }
        }
    }

    private static void ApplySibsMerchant(SibsApiSettings sibs, PspMerchantConfiguration row)
    {
        using var doc = JsonDocument.Parse(row.SettingsJson);
        var root = doc.RootElement;
        if (root.TryGetProperty("multibancoEntity", out var entity))
        {
            var value = entity.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                sibs.MerchantMultibancoEntities[row.MerchantId] = value;
        }
    }

    private static void ApplyWeroMerchant(WeroApiSettings wero, PspMerchantConfiguration row)
    {
        var accounts = JsonSerializer.Deserialize<WeroMerchantAccounts>(row.SettingsJson, JsonOptions);
        if (accounts is not null &&
            !string.IsNullOrWhiteSpace(accounts.DebtorIban) &&
            !string.IsNullOrWhiteSpace(accounts.CreditorIban))
        {
            wero.MerchantAccounts[row.MerchantId] = accounts;
        }
    }

    private static T DeserializeOrDefault<T>(string json, T fallback) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
