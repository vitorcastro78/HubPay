namespace HubPay.Domain.Configuration;

public class PspEndpointSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string WebhookBaseUrl { get; set; } = "https://api.hubpay.eu";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableSimulationFallback { get; set; } = true;
    public MutualTlsSettings MutualTls { get; set; } = new();
}

public sealed class MutualTlsSettings
{
    public bool Enabled { get; set; }
    public string ClientCertificatePath { get; set; } = string.Empty;
    public string ClientCertificatePassword { get; set; } = string.Empty;
    public string? ClientCertificatePasswordEnvironmentVariable { get; set; }
    public string? ClientPrivateKeyPath { get; set; }
    public string? CaCertificatePath { get; set; }
    public bool ValidateServerCertificate { get; set; } = true;
}
