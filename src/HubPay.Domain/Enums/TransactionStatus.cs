namespace HubPay.Domain.Enums;

public enum TransactionStatus
{
    Created = 0,
    Pending = 1,
    AntiFraudEvaluating = 2,
    BlockedByAntiFraud = 3,
    Authorized = 4,
    Settled = 5,
    Refunded = 6,
    Failed = 7
}
