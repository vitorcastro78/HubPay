using HubPay.Domain.Configuration;
using HubPay.Infrastructure.Payments.MutualTls;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HubPay.Tests.Unit.Payments;

public sealed class MutualTlsCertificateLoaderTests
{
    [Fact]
    public void LoadClientCertificate_ReturnsNull_WhenMtlsDisabled()
    {
        var loader = new MutualTlsCertificateLoader(NullLogger<MutualTlsCertificateLoader>.Instance);
        var cert = loader.LoadClientCertificate(new MutualTlsSettings { Enabled = false }, "SIBS");
        Assert.Null(cert);
    }

    [Fact]
    public void LoadClientCertificate_ReturnsNull_WhenFileMissing()
    {
        var loader = new MutualTlsCertificateLoader(NullLogger<MutualTlsCertificateLoader>.Instance);
        var cert = loader.LoadClientCertificate(new MutualTlsSettings
        {
            Enabled = true,
            ClientCertificatePath = "certificates/missing/client.pfx"
        }, "SIBS");

        Assert.Null(cert);
    }
}
