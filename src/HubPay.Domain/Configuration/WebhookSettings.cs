namespace HubPay.Domain.Configuration;

public sealed class WebhookSettings
{
    public string SignatureHeaderName { get; set; } = "X-HubPay-Signature";
    public Dictionary<string, string> SchemeSecrets { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MBWAY"] = "dev-webhook-secret-mbway",
        ["BIZUM"] = "dev-webhook-secret-bizum",
        ["WERO"] = "dev-webhook-secret-wero",
        ["MULTIBANCO"] = "dev-webhook-secret-multibanco",
        ["DEFAULT"] = "dev-webhook-secret-default"
    };
}
