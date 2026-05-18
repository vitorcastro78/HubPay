namespace HubPay.Application.DTOs;

public sealed record PaymentResponseDto(
    Guid TransactionId,
    string Status,
    string ScaStatus,
    decimal AntiFraudScore,
    string? RedirectUrl,
    string? ExternalReference,
    object? ChallengePayload,
    long AntiFraudElapsedMs,
    MultibancoDetailsDto? Multibanco = null,
    IdealDetailsDto? Ideal = null);

public sealed record MultibancoDetailsDto(string Entity, string Reference, DateTime DueDate);

public sealed record IdealDetailsDto(string QrPayload, string? RedirectUrl);
