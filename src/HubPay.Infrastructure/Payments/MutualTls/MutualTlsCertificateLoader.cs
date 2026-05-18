using System.Security.Cryptography.X509Certificates;
using HubPay.Domain.Configuration;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.MutualTls;

public sealed class MutualTlsCertificateLoader
{
    private readonly ILogger<MutualTlsCertificateLoader> _logger;

    public MutualTlsCertificateLoader(ILogger<MutualTlsCertificateLoader> logger) => _logger = logger;

    public X509Certificate2? LoadClientCertificate(MutualTlsSettings settings, string pspName)
    {
        if (!settings.Enabled)
            return null;

        var password = ResolvePassword(settings, pspName);
        var path = ResolvePath(settings.ClientCertificatePath);

        if (!File.Exists(path))
        {
            _logger.LogWarning("Certificado mTLS {Psp} não encontrado em {Path}", pspName, path);
            return null;
        }

        try
        {
            if (path.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".crt", StringComparison.OrdinalIgnoreCase))
            {
                return LoadFromPem(path, settings.ClientPrivateKeyPath, password);
            }

            var pfxBytes = File.ReadAllBytes(path);
            return X509CertificateLoader.LoadPkcs12(pfxBytes, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar certificado mTLS {Psp} de {Path}", pspName, path);
            throw;
        }
    }

    public X509Certificate2? LoadCaCertificate(MutualTlsSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.CaCertificatePath))
            return null;

        var path = ResolvePath(settings.CaCertificatePath);
        if (!File.Exists(path))
            return null;

        return X509CertificateLoader.LoadCertificate(File.ReadAllBytes(path));
    }

    private static string ResolvePassword(MutualTlsSettings settings, string pspName)
    {
        if (!string.IsNullOrWhiteSpace(settings.ClientCertificatePassword))
            return settings.ClientCertificatePassword;

        var envVar = settings.ClientCertificatePasswordEnvironmentVariable
                     ?? $"HUBPAY_{pspName.ToUpperInvariant()}_CERT_PASSWORD";

        return Environment.GetEnvironmentVariable(envVar) ?? string.Empty;
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(AppContext.BaseDirectory, path);
    }

    private static X509Certificate2 LoadFromPem(string certPath, string? keyPath, string? password)
    {
        var fullCertPath = ResolvePath(certPath);
        var certPem = File.ReadAllText(fullCertPath);

        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            var fullKeyPath = ResolvePath(keyPath);
            var keyPem = File.ReadAllText(fullKeyPath);
            return X509Certificate2.CreateFromEncryptedPem(certPem, keyPem, password);
        }

        return X509Certificate2.CreateFromPem(certPem);
    }
}
