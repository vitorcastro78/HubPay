using HubPay.Domain.Configuration;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments.MutualTls;

public static class PspMutualTlsDiagnostics
{
    public static void LogConfiguration(ILogger logger, HubPaySettings settings)
    {
        LogPsp(logger, "SIBS", settings.Sibs);
        LogPsp(logger, "Bizum", settings.Bizum);
        LogPsp(logger, "Wero", settings.Wero);
        LogPsp(logger, "iDEAL", settings.Ideal);
        LogPsp(logger, "Bancontact", settings.Bancontact);
        LogPsp(logger, "CartesBancaires", settings.CartesBancaires);
        LogPsp(logger, "Euro6000", settings.Euro6000);
        LogPsp(logger, "BancomatPay", settings.BancomatPay);
        LogPsp(logger, "Swish", settings.Swish);
        LogPsp(logger, "VippsMobilePay", settings.VippsMobilePay);
    }

    private static void LogPsp(ILogger logger, string name, PspEndpointSettings psp)
    {
        var mtls = psp.MutualTls;
        if (!mtls.Enabled)
        {
            logger.LogDebug("PSP {Name}: mTLS desativado, fallback={Fallback}", name, psp.EnableSimulationFallback);
            return;
        }

        var certPath = Path.IsPathRooted(mtls.ClientCertificatePath)
            ? mtls.ClientCertificatePath
            : Path.Combine(AppContext.BaseDirectory, mtls.ClientCertificatePath);

        if (File.Exists(certPath))
        {
            logger.LogInformation(
                "PSP {Name}: mTLS ativo, certificado em {Path}, validação servidor={Validate}",
                name, certPath, mtls.ValidateServerCertificate);
        }
        else
        {
            logger.LogWarning(
                "PSP {Name}: mTLS ativo mas certificado ausente em {Path}. Pedidos falharão se fallback estiver desativado.",
                name, certPath);
        }
    }
}
