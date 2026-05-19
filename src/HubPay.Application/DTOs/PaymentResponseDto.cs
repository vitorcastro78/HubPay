namespace HubPay.Application.DTOs;

/// <summary>Result of payment creation or refund.</summary>
/// <param name="TransactionId">Internal transaction GUID.</param>
/// <param name="Status">Transaction status (e.g. Pending, Completed, Failed).</param>
/// <param name="ScaStatus">SCA / TRA outcome from anti-fraud (e.g. Exempt, Challenge).</param>
/// <param name="AntiFraudScore">ONNX or fallback risk score (0–100).</param>
/// <param name="RedirectUrl">PSP redirect URL when applicable (3DS, app deep link).</param>
/// <param name="ExternalReference">PSP-side payment reference.</param>
/// <param name="ChallengePayload">Opaque PSP challenge data (3DS, etc.).</param>
/// <param name="AntiFraudElapsedMs">Anti-fraud inference latency in milliseconds.</param>
/// <param name="Multibanco">Multibanco reference details when scheme is MULTIBANCO.</param>
/// <param name="Ideal">iDEAL QR details when scheme is IDEAL.</param>
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

/// <summary>Multibanco payment reference returned by SIBS.</summary>
public sealed record MultibancoDetailsDto(string Entity, string Reference, DateTime DueDate);

/// <summary>iDEAL QR payment payload.</summary>
public sealed record IdealDetailsDto(string QrPayload, string? RedirectUrl);
