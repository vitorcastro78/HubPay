namespace HubPay.Application.DTOs;

public sealed record AntiFraudDetailDto(
    Guid TransactionId,
    decimal Amount,
    string CustomerIp,
    int DeviceTransactionsLast5Min,
    int EmailCountriesLastHour,
    float NormalizedAmount,
    float IpHashFeature,
    decimal FinalScore,
    long ElapsedMilliseconds,
    string ScaStatus,
    string Status,
    bool UsedFallback);
