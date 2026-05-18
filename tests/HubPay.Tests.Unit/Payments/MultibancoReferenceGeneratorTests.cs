namespace HubPay.Tests.Unit.Payments;

public sealed class MultibancoReferenceGeneratorTests
{
    [Fact]
    public void Generate_ReturnsValidEntityReferenceAndDueDate()
    {
        var (entity, reference, dueDate) = HubPay.Infrastructure.Payments.MultibancoReferenceGenerator.Generate(25.50m, "MERCH-1", "11683");
        Assert.Equal("11683", entity);
        Assert.Equal(11, reference.Length);
        Assert.True(dueDate > DateTime.UtcNow.Date);
    }

    [Fact]
    public void ComputeMod97CheckDigits_IsDeterministic()
    {
        var check = HubPay.Infrastructure.Payments.MultibancoReferenceGenerator.ComputeMod97CheckDigits("11683123456789");
        Assert.Equal(2, check.Length);
    }

    [Fact]
    public void Generate_InvalidEntity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            HubPay.Infrastructure.Payments.MultibancoReferenceGenerator.Generate(10m, "M1", "123"));
    }
}
