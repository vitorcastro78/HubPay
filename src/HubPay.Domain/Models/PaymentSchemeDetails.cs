namespace HubPay.Domain.Models;

public sealed record PaymentSchemeDetails(
    string? MultibancoEntity = null,
    string? MultibancoReference = null,
    DateTime? MultibancoDueDate = null,
    string? IdealQrPayload = null,
    object? ThreeDsChallenge = null);
