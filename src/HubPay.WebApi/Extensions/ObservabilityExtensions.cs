using HubPay.Infrastructure.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HubPay.WebApi.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddHubPayObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("HubPay.WebApi"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("HubPay.AntiFraud")
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(HubPayMetrics.MeterName)
                .AddConsoleExporter());

        return services;
    }
}
