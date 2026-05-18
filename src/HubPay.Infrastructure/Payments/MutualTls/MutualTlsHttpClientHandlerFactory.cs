using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using HubPay.Domain.Configuration;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.MutualTls;

public sealed class MutualTlsHttpClientHandlerFactory
{
    private readonly MutualTlsCertificateLoader _certificateLoader;
    private readonly ILogger<MutualTlsHttpClientHandlerFactory> _logger;

    public MutualTlsHttpClientHandlerFactory(
        MutualTlsCertificateLoader certificateLoader,
        ILogger<MutualTlsHttpClientHandlerFactory> logger)
    {
        _certificateLoader = certificateLoader;
        _logger = logger;
    }

    public HttpMessageHandler CreateHandler(PspEndpointSettings settings, string pspName)
    {
        var handler = new HttpClientHandler();
        ConfigureHandler(handler, settings, pspName);
        return handler;
    }

    public void ConfigureHandler(HttpClientHandler handler, PspEndpointSettings settings, string pspName)
    {
        if (!settings.MutualTls.Enabled)
            return;

        var clientCert = _certificateLoader.LoadClientCertificate(settings.MutualTls, pspName);
        if (clientCert is not null)
        {
            handler.ClientCertificates.Add(clientCert);
            _logger.LogInformation("mTLS ativado para {Psp} com certificado {Subject}", pspName, clientCert.Subject);
        }

        var caCert = _certificateLoader.LoadCaCertificate(settings.MutualTls);
        if (!settings.MutualTls.ValidateServerCertificate)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return;
        }

        if (caCert is not null)
        {
            handler.ServerCertificateCustomValidationCallback = (_, cert, chain, errors) =>
                ValidateWithCustomCa(cert, chain, errors, caCert);
        }
    }

    private static bool ValidateWithCustomCa(
        X509Certificate? serverCert,
        X509Chain? chain,
        SslPolicyErrors errors,
        X509Certificate2 caCert)
    {
        if (serverCert is null)
            return false;

        using var customChain = new X509Chain();
        customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        customChain.ChainPolicy.ExtraStore.Add(caCert);
        customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        customChain.ChainPolicy.CustomTrustStore.Add(caCert);

        return customChain.Build(new X509Certificate2(serverCert));
    }
}
