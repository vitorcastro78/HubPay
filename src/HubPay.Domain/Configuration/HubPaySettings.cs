namespace HubPay.Domain.Configuration;

public sealed class HubPaySettings
{
    public const string SectionName = "HubPay";

    public string ConnectionString { get; set; } = "Host=localhost;Database=hubpay;Username=postgres;Password=postgres";
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public string OnnxModelPath { get; set; } = "Models/antifraud.onnx";
    public decimal HubProcessingFeePercent { get; set; } = 0.8m;
    public int IdempotencyTtlHours { get; set; } = 24;
    public int ClearingIntervalSeconds { get; set; } = 30;
    public bool RequireMutualTlsInProduction { get; set; } = true;

    public SibsApiSettings Sibs { get; set; } = new();
    public BizumApiSettings Bizum { get; set; } = new();
    public WeroApiSettings Wero { get; set; } = new();
    public CartesBancairesApiSettings CartesBancaires { get; set; } = new();
    public PspEndpointSettings Ideal { get; set; } = new() { BaseUrl = "https://api.ideal.nl/v2" };
    public PspEndpointSettings Bancontact { get; set; } = new() { BaseUrl = "https://api.bancontact.com/v1" };
    public PspEndpointSettings Euro6000 { get; set; } = new() { BaseUrl = "https://api.euro6000.es/v1" };
    public PspEndpointSettings BancomatPay { get; set; } = new() { BaseUrl = "https://api.bancomatpay.it/v1" };
    public PspEndpointSettings Swish { get; set; } = new() { BaseUrl = "https://api.swish.nu/v2" };
    public PspEndpointSettings VippsMobilePay { get; set; } = new() { BaseUrl = "https://api.vipps.no/v1" };

    public JwtSettings Jwt { get; set; } = new();
    public WebhookSettings Webhooks { get; set; } = new();
    public Camt053Settings Camt053 { get; set; } = new();
    public bool ApplyMigrationsOnStartup { get; set; } = true;
}

public sealed class SibsApiSettings : PspEndpointSettings
{
    public SibsApiSettings()
    {
        BaseUrl = "https://api.sibs.pt";
        ClientId = "hubpay-sibs-client";
        MutualTls = new MutualTlsSettings
        {
            Enabled = true,
            ClientCertificatePath = "certificates/sibs/client.pfx",
            ClientCertificatePasswordEnvironmentVariable = "HUBPAY_SIBS_CERT_PASSWORD"
        };
    }

    public string MbWayInitPath { get; set; } = "/v1/mbway/payments";
    public string MultibancoInitPath { get; set; } = "/v1/multibanco/references";
    public string RefundPath { get; set; } = "/v1/payments/{paymentId}/refunds";
    public string DefaultMultibancoEntity { get; set; } = "11683";
    public Dictionary<string, string> MerchantMultibancoEntities { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string ResolveMultibancoEntity(string merchantId) =>
        MerchantMultibancoEntities.TryGetValue(merchantId, out var entity) ? entity : DefaultMultibancoEntity;
}

public sealed class BizumApiSettings : PspEndpointSettings
{
    public BizumApiSettings()
    {
        BaseUrl = "https://api.bizum.es";
        ClientId = "hubpay-bizum-client";
        MutualTls = new MutualTlsSettings
        {
            Enabled = true,
            ClientCertificatePath = "certificates/bizum/client.pfx",
            ClientCertificatePasswordEnvironmentVariable = "HUBPAY_BIZUM_CERT_PASSWORD"
        };
    }

    public string PaymentInitPath { get; set; } = "/v1/payments";
    public string RefundPath { get; set; } = "/v1/payments/{paymentId}/refunds";
}

public sealed class WeroApiSettings : PspEndpointSettings
{
    public WeroApiSettings()
    {
        BaseUrl = "https://api.wero.eu";
        ClientId = "hubpay-wero-client";
        MutualTls = new MutualTlsSettings
        {
            Enabled = true,
            ClientCertificatePath = "certificates/wero/client.pfx",
            ClientCertificatePasswordEnvironmentVariable = "HUBPAY_WERO_CERT_PASSWORD"
        };
    }

    public string InstantPaymentPath { get; set; } = "/v1/instant-payments";
    public string RefundPath { get; set; } = "/v1/instant-payments/{paymentId}/refunds";
    public string DefaultDebtorIban { get; set; } = "DE89370400440532013000";
    public string DefaultCreditorIban { get; set; } = "FR1420041010050500013M02606";
    public Dictionary<string, WeroMerchantAccounts> MerchantAccounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public (string DebtorIban, string CreditorIban) ResolveAccounts(string merchantId)
    {
        if (MerchantAccounts.TryGetValue(merchantId, out var accounts))
            return (accounts.DebtorIban, accounts.CreditorIban);
        return (DefaultDebtorIban, DefaultCreditorIban);
    }
}

public sealed class WeroMerchantAccounts
{
    public string DebtorIban { get; set; } = string.Empty;
    public string CreditorIban { get; set; } = string.Empty;
}

public sealed class CartesBancairesApiSettings : PspEndpointSettings
{
    public CartesBancairesApiSettings()
    {
        BaseUrl = "https://api.cartesbancaires.fr";
        MutualTls = new MutualTlsSettings
        {
            Enabled = true,
            ClientCertificatePath = "certificates/cartesbancaires/client.pfx",
            ClientCertificatePasswordEnvironmentVariable = "HUBPAY_CB_CERT_PASSWORD"
        };
    }

    public string AuthorizePath { get; set; } = "/v1/payments/authorize";
    public string ThreeDsStatusPath { get; set; } = "/v1/payments/{paymentId}/3ds/status";
}
