namespace HubPay.Domain.Models;

public sealed record AntiFraudEvaluationResult(
    decimal Score,
    string ScaStatus,
    long ElapsedMilliseconds,
    AntiFraudInputFeatures Features,
    bool UsedFallback);

public sealed record AntiFraudInputFeatures(
    decimal Amount,
    string CustomerIp,
    int DeviceTransactionsLast5Min,
    int EmailCountriesLastHour,
    float NormalizedAmount,
    float IpHashFeature);
