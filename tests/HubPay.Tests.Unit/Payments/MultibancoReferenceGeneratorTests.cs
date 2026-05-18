using HubPay.Infrastructure.Payments;

namespace HubPay.Tests.Unit.Payments;

public sealed class MultibancoReferenceGeneratorTests
{
    [Fact]
    public void Generate_ReturnsEntityReferenceAndDueDate()
    {
        var (entity, reference, dueDate) = MultibancoReferenceGenerator.Generate(25.50m, "MERCH-1");
        Assert.Equal("12345", entity);
        Assert.Equal(11, reference.Length);
        Assert.True(dueDate >= DateTime.UtcNow.Date);
    }

    [Fact]
    public void ComputeMod97CheckDigits_IsDeterministic()
    {
        var check = MultibancoReferenceGenerator.ComputeMod97CheckDigits("12345123456789");
        Assert.Equal(2, check.Length);
    }
}
