using HubPay.Domain.Enums;
using HubPay.Domain.Exceptions;

namespace HubPay.Domain.Entities;

public sealed class Transaction
{
    private Transaction() { }

    public Guid Id { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public string PaymentScheme { get; private set; } = string.Empty;
    public string EndToEndId { get; private set; } = string.Empty;
    public string CustomerIP { get; private set; } = string.Empty;
    public string DeviceFingerprint { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string? CustomerPhone { get; private set; }
    public string? PspMetadataJson { get; private set; }
    public string ScaStatus { get; private set; } = string.Empty;
    public decimal AntiFraudScore { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? ExternalReference { get; private set; }
    public decimal? NetSettledAmount { get; private set; }
    public decimal? ProcessingFee { get; private set; }
    public long AntiFraudElapsedMs { get; private set; }
    public string? CountryCode { get; private set; }

    public static Transaction Create(
        string merchantId,
        decimal amount,
        string currency,
        string paymentScheme,
        string endToEndId,
        string customerIp,
        string deviceFingerprint,
        string customerEmail,
        string? customerPhone = null,
        string? countryCode = null)
    {
        if (amount <= 0)
            throw new BusinessRuleException("O montante deve ser superior a zero.");
        if (!string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("A moeda deve ser estritamente EUR.");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            PaymentScheme = paymentScheme,
            EndToEndId = endToEndId,
            CustomerIP = customerIp,
            DeviceFingerprint = deviceFingerprint,
            CustomerEmail = customerEmail,
            CustomerPhone = NormalizePhone(customerPhone),
            ScaStatus = "NONE",
            AntiFraudScore = 0m,
            Status = TransactionStatus.Created,
            CreatedAt = DateTime.UtcNow,
            CountryCode = countryCode
        };
    }

    public void SetPspMetadata(string metadataJson)
    {
        PspMetadataJson = metadataJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartAntiFraudEvaluation()
    {
        EnsureStatus(TransactionStatus.Created, "Apenas transações Created podem iniciar avaliação anti-fraude.");
        Status = TransactionStatus.AntiFraudEvaluating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyAntiFraudResult(decimal score, string scaStatus, long elapsedMs)
    {
        EnsureStatus(TransactionStatus.AntiFraudEvaluating, "Avaliação anti-fraude só é válida em AntiFraudEvaluating.");
        AntiFraudScore = score;
        ScaStatus = scaStatus;
        AntiFraudElapsedMs = elapsedMs;
        UpdatedAt = DateTime.UtcNow;

        if (score > 70m)
        {
            Status = TransactionStatus.BlockedByAntiFraud;
            throw new BusinessRuleException($"Transação bloqueada por anti-fraude. Score: {score:F2}");
        }

        if (score >= 15m && score <= 70m)
        {
            Status = TransactionStatus.Pending;
            return;
        }

        Status = TransactionStatus.Authorized;
    }

    public void MarkPending(string externalReference)
    {
        Status = TransactionStatus.Pending;
        ExternalReference = externalReference;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Authorize(string? externalReference = null)
    {
        if (Status is not (TransactionStatus.AntiFraudEvaluating or TransactionStatus.Pending))
            throw new BusinessRuleException("Autorização inválida para o estado atual.");
        Status = TransactionStatus.Authorized;
        if (externalReference is not null)
            ExternalReference = externalReference;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Settle(decimal netAmount, decimal fee)
    {
        EnsureStatus(TransactionStatus.Authorized, "Apenas transações Authorized podem ser liquidadas.");
        NetSettledAmount = netAmount;
        ProcessingFee = fee;
        Status = TransactionStatus.Settled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Refund()
    {
        if (Status is not (TransactionStatus.Authorized or TransactionStatus.Settled))
            throw new BusinessRuleException("Apenas transações Authorized ou Settled podem ser reembolsadas.");
        Status = TransactionStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = TransactionStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
        _ = reason;
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var trimmed = phone.Trim();
        return trimmed.StartsWith('+') ? trimmed : $"+{trimmed}";
    }

    private void EnsureStatus(TransactionStatus expected, string message)
    {
        if (Status != expected)
            throw new BusinessRuleException(message);
    }
}
