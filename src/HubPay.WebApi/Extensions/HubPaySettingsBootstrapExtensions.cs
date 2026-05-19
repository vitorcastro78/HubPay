namespace HubPay.WebApi.Extensions;

public static class HubPaySettingsBootstrapExtensions
{
    public static async Task<HubPay.Domain.Configuration.HubPaySettings> LoadHubPaySettingsFromDatabaseAsync(
        this WebApplication app,
        CancellationToken ct = default) =>
        await HubPaySettingsBootstrap.LoadFromDatabaseAsync(app.Services, app.Configuration, ct);
}
