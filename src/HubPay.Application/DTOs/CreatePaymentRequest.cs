namespace HubPay.Application.DTOs;

/// <summary>Request body for initiating a payment through the hub.</summary>
/// <param name="MerchantId">Calling merchant identifier (max 64 characters).</param>
/// <param name="Amount">Payment amount; must be greater than zero.</param>
/// <param name="Currency">ISO 4217 currency code; only <c>EUR</c> is supported.</param>
/// <param name="PaymentScheme">
/// Payment rail: MBWAY, MULTIBANCO, BIZUM, EURO6000, WERO, CARTESBANCAIRES, IDEAL,
/// BANCONTACT, BANCOMATPAY, SWISH, VIPPSMOBILEPAY.
/// </param>
/// <param name="EndToEndId">Unique end-to-end reference (max 35 characters).</param>
/// <param name="CustomerIP">Customer IP address (IPv4 or IPv6).</param>
/// <param name="DeviceFingerprint">Opaque device fingerprint for anti-fraud.</param>
/// <param name="CustomerEmail">Customer email address.</param>
/// <param name="CountryCode">Optional ISO 3166-1 alpha-2 country code.</param>
/// <param name="PhoneNumber">
/// E.164 phone number (e.g. +351912345678). Required for MBWAY, BIZUM, and BANCOMATPAY.
/// </param>
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
