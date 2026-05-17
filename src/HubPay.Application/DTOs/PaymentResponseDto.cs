namespace HubPay.Application.DTOs;

public sealed record PaymentResponseDto(
    Guid TransactionId,
    string Status,
    string ScaStatus,
    decimal AntiFraudScore,
    string? RedirectUrl,
    string? ExternalReference,
    object? ChallengePayload,
    long AntiFraudElapsedMs);
