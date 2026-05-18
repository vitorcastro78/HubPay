using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Configuration;

public sealed class ConfigureHubPaySettingsFromDatabase : IConfigureOptions<HubPaySettings>
{
    private readonly IHubPaySettingsProvider _provider;

    public ConfigureHubPaySettingsFromDatabase(IHubPaySettingsProvider provider) => _provider = provider;

    public void Configure(HubPaySettings options)
    {
        if (!_provider.IsInitialized)
            return;

        var current = _provider.Current;
        options.ConnectionString = current.ConnectionString;
        options.RedisConnectionString = current.RedisConnectionString;
        options.OnnxModelPath = current.OnnxModelPath;
        options.HubProcessingFeePercent = current.HubProcessingFeePercent;
        options.IdempotencyTtlHours = current.IdempotencyTtlHours;
        options.ClearingIntervalSeconds = current.ClearingIntervalSeconds;
        options.RequireMutualTlsInProduction = current.RequireMutualTlsInProduction;
        options.ApplyMigrationsOnStartup = current.ApplyMigrationsOnStartup;
        options.Jwt = current.Jwt;
        options.Camt053 = current.Camt053;
        options.Webhooks = current.Webhooks;
        options.Sibs = current.Sibs;
        options.Bizum = current.Bizum;
        options.Wero = current.Wero;
        options.CartesBancaires = current.CartesBancaires;
        options.Ideal = current.Ideal;
        options.Bancontact = current.Bancontact;
        options.Euro6000 = current.Euro6000;
        options.BancomatPay = current.BancomatPay;
        options.Swish = current.Swish;
        options.VippsMobilePay = current.VippsMobilePay;
    }
}
