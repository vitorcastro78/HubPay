using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using HubPay.Application.Interfaces;
using HubPay.Infrastructure.Configuration;
using HubPay.Infrastructure.AntiFraud;
using HubPay.Infrastructure.Clearing;
using HubPay.Infrastructure.Notifications;
using HubPay.Infrastructure.Webhooks;
using HubPay.Infrastructure.Payments;
using HubPay.Infrastructure.Payments.MutualTls;
using HubPay.Infrastructure.Payments.Strategies;
using HubPay.Infrastructure.Persistence;
using HubPay.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

namespace HubPay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HubPaySettings>(configuration.GetSection(HubPaySettings.SectionName));
        services.AddSingleton<IHubPaySettingsProvider, HubPaySettingsProvider>();
        services.AddSingleton<IConfigureOptions<HubPaySettings>, ConfigureHubPaySettingsFromDatabase>();
        services.AddScoped<DatabaseHubPaySettingsLoader>();
        services.AddScoped<PspConfigurationSeeder>();
        services.AddScoped<IPspConfigurationAdminService, PspConfigurationAdminService>();

        var settings = configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()
                       ?? new HubPaySettings();

        services.AddDbContext<HubPayDbContext>(options =>
            options.UseNpgsql(settings.ConnectionString));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisOptions = ConfigurationOptions.Parse(settings.RedisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisOptions);
        });

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<RedisFeatureStore>();
        services.AddSingleton<IAntiFraudAuditStore, RedisAntiFraudAuditStore>();
        services.AddSingleton<IAntiFraudEngine, AntiFraudEngine>();

        services.AddSingleton<MutualTlsCertificateLoader>();
        services.AddSingleton<MutualTlsHttpClientHandlerFactory>();

        RegisterPspClient<MBWayStrategy>(services, s => s.Sibs, "SIBS");
        RegisterPspClient<MultibancoStrategy>(services, s => s.Sibs, "SIBS");
        RegisterPspClient<BizumStrategy>(services, s => s.Bizum, "Bizum");
        RegisterPspClient<Euro6000Strategy>(services, s => s.Euro6000, "Euro6000");
        RegisterPspClient<WeroStrategy>(services, s => s.Wero, "Wero");
        RegisterPspClient<CartesBancairesStrategy>(services, s => s.CartesBancaires, "CartesBancaires");
        RegisterPspClient<IDealStrategy>(services, s => s.Ideal, "iDEAL");
        RegisterPspClient<BancontactStrategy>(services, s => s.Bancontact, "Bancontact");
        RegisterPspClient<BancomatPayStrategy>(services, s => s.BancomatPay, "BancomatPay");
        RegisterPspClient<SwishStrategy>(services, s => s.Swish, "Swish");
        RegisterPspClient<VippsMobilePayStrategy>(services, s => s.VippsMobilePay, "VippsMobilePay");

        services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();
        services.AddSingleton<ICamt053Parser, Camt053Parser>();
        services.AddSingleton<ICamt053StatementReader, FileCamt053StatementReader>();
        services.AddSingleton<IWebhookSignatureValidator, HmacWebhookSignatureValidator>();
        services.AddScoped<ITransactionNotifier, NullTransactionNotifier>();
        services.AddHostedService<FinancialClearingEngine>();
        services.AddHostedService<PspProductionValidator>();

        return services;
    }

    private static void RegisterPspClient<TStrategy>(
        IServiceCollection services,
        Func<HubPaySettings, PspEndpointSettings> settingsSelector,
        string pspName)
        where TStrategy : class, IPaymentStrategy
    {
        services.AddHttpClient<TStrategy>(typeof(TStrategy).Name)
            .ConfigurePspClient(settingsSelector, pspName)
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddScoped<IPaymentStrategy, TStrategy>(sp => sp.GetRequiredService<TStrategy>());
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, retry => TimeSpan.FromMilliseconds(100 * retry));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
