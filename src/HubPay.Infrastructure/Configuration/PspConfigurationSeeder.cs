using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Configuration;

public sealed class PspConfigurationSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly HubPayDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PspConfigurationSeeder> _logger;

    public PspConfigurationSeeder(
        HubPayDbContext db,
        IConfiguration configuration,
        ILogger<PspConfigurationSeeder> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedIfEmptyAsync(CancellationToken ct = default)
    {
        if (await _db.PspProviderConfigurations.AnyAsync(ct))
            return;

        var fromConfig = _configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()
                         ?? new HubPaySettings();

        var now = DateTime.UtcNow;
        var rows = new List<PspProviderConfiguration>
        {
            Create("SIBS", fromConfig.Sibs, now),
            Create("BIZUM", fromConfig.Bizum, now),
            Create("WERO", fromConfig.Wero, now),
            Create("CARTESBANCAIRES", fromConfig.CartesBancaires, now),
            Create("IDEAL", fromConfig.Ideal, now),
            Create("BANCONTACT", fromConfig.Bancontact, now),
            Create("EURO6000", fromConfig.Euro6000, now),
            Create("BANCOMATPAY", fromConfig.BancomatPay, now),
            Create("SWISH", fromConfig.Swish, now),
            Create("VIPPSMOBILEPAY", fromConfig.VippsMobilePay, now),
            Create("WEBHOOKS", fromConfig.Webhooks, now)
        };

        await _db.PspProviderConfigurations.AddRangeAsync(rows, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seed inicial de {Count} configurações PSP gravado na BD.", rows.Count);
    }

    private static PspProviderConfiguration Create<T>(string scheme, T settings, DateTime updatedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Scheme = scheme,
            IsEnabled = true,
            SettingsJson = JsonSerializer.Serialize(settings, JsonOptions),
            UpdatedAt = updatedAt
        };
}
