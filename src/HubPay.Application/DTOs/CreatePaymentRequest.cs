namespace HubPay.Application.DTOs;

public sealed record CreatePaymentRequest(
    string MerchantId,
    decimal Amount,
    string Currency,
    string PaymentScheme,
    string EndToEndId,
    string CustomerIP,
    string DeviceFingerprint,
    string CustomerEmail,
    string? CountryCode,
    string? PhoneNumber);
