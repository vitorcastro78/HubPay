namespace HubPay.Domain.Interfaces;

public interface IWebhookSignatureValidator
{
    bool Validate(string scheme, string payload, string? signatureHeader);
}
