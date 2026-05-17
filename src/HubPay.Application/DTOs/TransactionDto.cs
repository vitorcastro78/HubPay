namespace HubPay.Application.DTOs;

public sealed record TransactionDto(
    Guid Id,
    string MerchantId,
    decimal Amount,
    string Currency,
    string PaymentScheme,
    string EndToEndId,
    string Status,
    string ScaStatus,
    decimal AntiFraudScore,
    string? CountryCode,
    DateTime CreatedAt,
    long AntiFraudElapsedMs);
