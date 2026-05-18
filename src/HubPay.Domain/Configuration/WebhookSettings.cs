namespace HubPay.Domain.Configuration;

public sealed class WebhookSettings
{
    public string SignatureHeaderName { get; set; } = "X-HubPay-Signature";

    public Dictionary<string, string> SchemeSecrets { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MBWAY"] = "",
        ["MULTIBANCO"] = "",
        ["BIZUM"] = "",
        ["WERO"] = "",
        ["EURO6000"] = "",
        ["CARTESBANCAIRES"] = "",
        ["IDEAL"] = "",
        ["BANCONTACT"] = "",
        ["BANCOMATPAY"] = "",
        ["SWISH"] = "",
        ["VIPPSMOBILEPAY"] = "",
        ["DEFAULT"] = ""
    };
}
