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

    public SibsApiSettings Sibs { get; set; } = new();
    public BizumApiSettings Bizum { get; set; } = new();
    public WeroApiSettings Wero { get; set; } = new();
    public PspEndpointSettings Ideal { get; set; } = new() { BaseUrl = "https://api.ideal.nl/v2" };
    public PspEndpointSettings Bancontact { get; set; } = new() { BaseUrl = "https://api.bancontact.com/v1" };
    public PspEndpointSettings CartesBancaires { get; set; } = new() { BaseUrl = "https://api.cartesbancaires.fr/v1" };
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
        BaseUrl = "https://api.sibs.pt/sandbox";
        ApiKey = "fake-sibs-api-key";
        ClientId = "hubpay-sibs-client";
        MutualTls = new MutualTlsSettings
        {
            Enabled = false,
            ClientCertificatePath = "certificates/sibs/client.pfx",
            ClientCertificatePasswordEnvironmentVariable = "HUBPAY_SIBS_CERT_PASSWORD"
        };
    }

    public string MbWayInitPath { get; set; } = "/v1/mbway/payments";
    public string MultibancoInitPath { get; set; } = "/v1/multibanco/references";
    public string RefundPath { get; set; } = "/v1/payments/{paymentId}/refunds";
}

public sealed class BizumApiSettings : PspEndpointSettings
{
    public BizumApiSettings()
    {
        BaseUrl = "https://api.bizum.es/sandbox";
        ApiKey = "fake-bizum-api-key";
        ClientId = "hubpay-bizum-client";
        MutualTls = new MutualTlsSettings
        {
            Enabled = false,
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
        BaseUrl = "https://api.wero.eu/sandbox";
        ApiKey = "fake-wero-api-key";
        ClientId = "hubpay-wero-client";
        MutualTls = new MutualTlsSettings
        {
            Enabled = false,
            ClientCertificatePath = "certificates/wero/client.pfx",
            ClientCertificatePasswordEnvironmentVariable = "HUBPAY_WERO_CERT_PASSWORD"
        };
    }

    public string InstantPaymentPath { get; set; } = "/v1/instant-payments";
    public string RefundPath { get; set; } = "/v1/instant-payments/{paymentId}/refunds";
    public string DebtorIban { get; set; } = "DE89370400440532013000";
    public string CreditorIban { get; set; } = "FR1420041010050500013M02606";
}
