using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.Configuration;
using HubPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HubPay.WebApi.Extensions;

public static class HubPaySettingsBootstrapExtensions
{
    public static async Task<HubPaySettings> LoadHubPaySettingsFromDatabaseAsync(this WebApplication app)
    {
        var bootstrap = app.Configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()
                        ?? new HubPaySettings();
        var loadFromDatabase = app.Configuration.GetValue("HubPay:LoadPspConfigurationFromDatabase", true);
        var provider = app.Services.GetRequiredService<IHubPaySettingsProvider>();

        if (!loadFromDatabase)
        {
            provider.Initialize(bootstrap);
            return app.Services.GetRequiredService<IOptions<HubPaySettings>>().Value;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HubPayDbContext>();

        if (bootstrap.ApplyMigrationsOnStartup && db.Database.IsRelational())
            await db.Database.MigrateAsync();

        await scope.ServiceProvider.GetRequiredService<PspConfigurationSeeder>().SeedIfEmptyAsync();
        var settings = await scope.ServiceProvider.GetRequiredService<DatabaseHubPaySettingsLoader>().LoadAsync();
        provider.Initialize(settings);

        return app.Services.GetRequiredService<IOptions<HubPaySettings>>().Value;
    }
}
