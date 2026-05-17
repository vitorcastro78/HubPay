namespace HubPay.Infrastructure.Payments;

public static class MultibancoReferenceGenerator
{
    public static (string Entity, string Reference, DateTime DueDate) Generate(decimal amount, string merchantId)
    {
        const string entity = "12345";
        var random = new Random(merchantId.GetHashCode() ^ (int)(amount * 100));
        var baseRef = random.Next(100000000, 999999999).ToString();
        var checkDigits = ComputeMod97CheckDigits(entity + baseRef);
        var reference = baseRef + checkDigits;
        return (entity, reference, DateTime.UtcNow.Date.AddDays(3));
    }

    public static string ComputeMod97CheckDigits(string input)
    {
        var numeric = string.Concat(input.Select(c => char.IsDigit(c) ? c.ToString() : (c - 'A' + 10).ToString()));
        var remainder = 0;
        foreach (var ch in numeric)
        {
            remainder = (remainder * 10 + (ch - '0')) % 97;
        }
        var check = 98 - remainder;
        return check.ToString("D2");
    }
}
