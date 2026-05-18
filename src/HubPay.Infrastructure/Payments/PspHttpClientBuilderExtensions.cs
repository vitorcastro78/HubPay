using HubPay.Domain.Configuration;
using HubPay.Infrastructure.Payments.MutualTls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments;

public static class PspHttpClientBuilderExtensions
{
    public static IHttpClientBuilder ConfigurePspClient<TSettings>(
        this IHttpClientBuilder builder,
        Func<HubPaySettings, TSettings> settingsSelector,
        string pspName,
        Action<HttpClient, TSettings>? configureClient = null)
        where TSettings : PspEndpointSettings
    {
        return builder.ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var hubSettings = sp.GetRequiredService<IOptions<HubPaySettings>>().Value;
            var pspSettings = settingsSelector(hubSettings);
            var factory = sp.GetRequiredService<MutualTlsHttpClientHandlerFactory>();
            return factory.CreateHandler(pspSettings, pspName);
        }).ConfigureHttpClient((sp, client) =>
        {
            var hubSettings = sp.GetRequiredService<IOptions<HubPaySettings>>().Value;
            var pspSettings = settingsSelector(hubSettings);
            client.BaseAddress = new Uri(pspSettings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(pspSettings.TimeoutSeconds);
            configureClient?.Invoke(client, pspSettings);
        });
    }
}
