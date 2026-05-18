using HubPay.Domain.Entities;
using HubPay.Domain.Enums;
using HubPay.Domain.Exceptions;

namespace HubPay.Tests.Unit.Domain;

public sealed class TransactionStateMachineTests
{
    [Fact]
    public void Create_ValidRequest_SetsCreatedStatus()
    {
        var tx = Transaction.Create("M1", 10m, "EUR", "MBWAY", "E2E-1", "1.1.1.1", "fp", "a@b.com");
        Assert.Equal(TransactionStatus.Created, tx.Status);
    }

    [Fact]
    public void Create_NonEurCurrency_Throws()
    {
        Assert.Throws<BusinessRuleException>(() =>
            Transaction.Create("M1", 10m, "USD", "MBWAY", "E2E-1", "1.1.1.1", "fp", "a@b.com"));
    }

    [Fact]
    public void AntiFraud_FromCreated_TransitionsToEvaluating()
    {
        var tx = Transaction.Create("M1", 10m, "EUR", "MBWAY", "E2E-1", "1.1.1.1", "fp", "a@b.com");
        tx.StartAntiFraudEvaluation();
        Assert.Equal(TransactionStatus.AntiFraudEvaluating, tx.Status);
    }

    [Fact]
    public void ApplyAntiFraud_HighScore_BlocksTransaction()
    {
        var tx = Transaction.Create("M1", 100m, "EUR", "MBWAY", "E2E-1", "1.1.1.1", "fp", "a@b.com");
        tx.StartAntiFraudEvaluation();
        var ex = Assert.Throws<BusinessRuleException>(() => tx.ApplyAntiFraudResult(85m, "BLOCKED", 5));
        Assert.Equal(TransactionStatus.BlockedByAntiFraud, tx.Status);
        Assert.NotNull(ex);
    }

    [Fact]
    public void Settle_FromAuthorized_UpdatesAmounts()
    {
        var tx = Transaction.Create("M1", 100m, "EUR", "MBWAY", "E2E-1", "1.1.1.1", "fp", "a@b.com");
        tx.StartAntiFraudEvaluation();
        tx.ApplyAntiFraudResult(10m, "TRA", 5);
        tx.Settle(99.2m, 0.8m);
        Assert.Equal(TransactionStatus.Settled, tx.Status);
        Assert.Equal(99.2m, tx.NetSettledAmount);
    }
}
