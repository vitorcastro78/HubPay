using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.AntiFraud;
using HubPay.Infrastructure.Clearing;
using HubPay.Infrastructure.Payments;
using HubPay.Infrastructure.Payments.Strategies;
using HubPay.Infrastructure.Persistence;
using HubPay.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

namespace HubPay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HubPaySettings>(configuration.GetSection(HubPaySettings.SectionName));

        var settings = configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()
                       ?? new HubPaySettings();

        services.AddDbContext<HubPayDbContext>(options =>
            options.UseNpgsql(settings.ConnectionString));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(settings.RedisConnectionString));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<RedisFeatureStore>();
        services.AddScoped<IAntiFraudAuditStore, RedisAntiFraudAuditStore>();
        services.AddSingleton<IAntiFraudEngine, AntiFraudEngine>();

        RegisterPaymentStrategy<MBWayStrategy>(services, "MBWay");
        RegisterPaymentStrategy<MultibancoStrategy>(services, "Multibanco");
        RegisterPaymentStrategy<BizumStrategy>(services, "Bizum");
        RegisterPaymentStrategy<Euro6000Strategy>(services, "Euro6000");
        RegisterPaymentStrategy<WeroStrategy>(services, "Wero");
        RegisterPaymentStrategy<CartesBancairesStrategy>(services, "CartesBancaires");
        RegisterPaymentStrategy<IDealStrategy>(services, "IDeal");
        RegisterPaymentStrategy<BancontactStrategy>(services, "Bancontact");
        RegisterPaymentStrategy<BancomatPayStrategy>(services, "BancomatPay");
        RegisterPaymentStrategy<SwishStrategy>(services, "Swish");
        RegisterPaymentStrategy<VippsMobilePayStrategy>(services, "VippsMobilePay");

        services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();
        services.AddHostedService<FinancialClearingEngine>();

        return services;
    }

    private static void RegisterPaymentStrategy<T>(IServiceCollection services, string name)
        where T : class, IPaymentStrategy
    {
        services.AddHttpClient<T>(name)
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        services.AddScoped<IPaymentStrategy, T>(sp => sp.GetRequiredService<T>());
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
