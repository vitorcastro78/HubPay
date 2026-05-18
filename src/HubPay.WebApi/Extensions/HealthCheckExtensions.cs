using HubPay.Domain.Configuration;
using HubPay.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HubPay.WebApi.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHubPayHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>() ?? new HubPaySettings();

        services.AddHealthChecks()
            .AddNpgSql(settings.ConnectionString, name: "postgresql", tags: ["db", "ready"])
            .AddRedis(settings.RedisConnectionString, name: "redis", tags: ["cache", "ready"])
            .AddCheck<OnnxModelHealthCheck>("onnx-model", tags: ["antifraud", "ready"]);

        return services;
    }

    public static WebApplication MapHubPayHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        return app;
    }
}

public sealed class OnnxModelHealthCheck : IHealthCheck
{
    private readonly HubPaySettings _settings;

    public OnnxModelHealthCheck(Microsoft.Extensions.Options.IOptions<HubPaySettings> options) =>
        _settings = options.Value;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var path = _settings.OnnxModelPath;
        if (!Path.IsPathRooted(path))
            path = Path.Combine(AppContext.BaseDirectory, path);

        if (File.Exists(path))
            return Task.FromResult(HealthCheckResult.Healthy("Modelo ONNX disponível."));

        return Task.FromResult(HealthCheckResult.Degraded("Modelo ONNX ausente; motor matemático/fallback ativo."));
    }
}
