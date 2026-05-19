using HubPay.Domain.Configuration;
using HubPay.Infrastructure.Persistence;
using HubPay.WebApi.OpenApi;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HubPay.WebApi.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHubPayHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>() ?? new HubPaySettings();
        var connectionString = configuration.GetConnectionString("hubpay") ?? settings.ConnectionString;
        var redisConnectionString = configuration.GetConnectionString("redis") ?? settings.RedisConnectionString;

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql", tags: ["db", "ready"], timeout: TimeSpan.FromSeconds(5))
            .AddRedis(redisConnectionString, name: "redis", tags: ["cache", "ready"], timeout: TimeSpan.FromSeconds(3))
            .AddCheck<OnnxModelHealthCheck>("onnx-model", tags: ["antifraud", "ready"]);

        return services;
    }

    public static WebApplication MapHubPayHealthChecks(this WebApplication app)
    {
        // Fast liveness probe for Aspire / orchestrators (no PostgreSQL or Redis).
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            })
            .WithTags(HubPayApiDescriptions.TagHealth)
            .WithName("HealthLive")
            .AllowAnonymous();

        app.MapHealthChecks("/health")
            .WithTags(HubPayApiDescriptions.TagHealth)
            .WithName("Health")
            .WithSummary(HubPayApiDescriptions.HealthSummary)
            .WithDescription(HubPayApiDescriptions.HealthDescription)
            .AllowAnonymous();

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            })
            .WithTags(HubPayApiDescriptions.TagHealth)
            .WithName("HealthReady")
            .WithSummary(HubPayApiDescriptions.HealthReadySummary)
            .WithDescription(HubPayApiDescriptions.HealthReadyDescription)
            .AllowAnonymous();

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
