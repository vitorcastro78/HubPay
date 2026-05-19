using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.Configuration;
using HubPay.Infrastructure.Payments.MutualTls;
using HubPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HubPay.WebApi.Extensions;

/// <summary>
/// Loads PSP settings from PostgreSQL after the host starts so Aspire liveness probes are not blocked.
/// </summary>
public sealed class HubPaySettingsBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HubPaySettingsBootstrapHostedService> _logger;

    public HubPaySettingsBootstrapHostedService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<HubPaySettingsBootstrapHostedService> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await HubPaySettingsBootstrap.LoadFromDatabaseAsync(_services, _configuration, cancellationToken);
            PspMutualTlsDiagnostics.LogConfiguration(_logger, settings);
            _logger.LogInformation("Bootstrap PSP concluído.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootstrap PSP falhou; a API continua com appsettings.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class HubPaySettingsBootstrap
{
    public static async Task<HubPaySettings> LoadFromDatabaseAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken ct = default)
    {
        var bootstrap = configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()
                        ?? new HubPaySettings();
        var loadFromDatabase = configuration.GetValue("HubPay:LoadPspConfigurationFromDatabase", true);
        var provider = services.GetRequiredService<IHubPaySettingsProvider>();

        if (!loadFromDatabase)
        {
            provider.Initialize(bootstrap);
            return services.GetRequiredService<IOptions<HubPaySettings>>().Value;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HubPayDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("HubPay.DatabaseBootstrap");

        if (bootstrap.ApplyMigrationsOnStartup && db.Database.IsRelational())
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation(
                    "A aplicar {Count} migration(s) pendente(s): {Migrations}",
                    pending.Count,
                    string.Join(", ", pending));
            }

            await db.Database.MigrateAsync(ct);
        }

        await scope.ServiceProvider.GetRequiredService<PspConfigurationSeeder>().SeedIfEmptyAsync(ct);
        var settings = await scope.ServiceProvider.GetRequiredService<DatabaseHubPaySettingsLoader>().LoadAsync(ct);
        provider.Initialize(settings);

        return services.GetRequiredService<IOptions<HubPaySettings>>().Value;
    }
}
