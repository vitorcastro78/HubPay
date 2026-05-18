namespace HubPay.Infrastructure.Payments;

public static class IdealQrBuilder
{
    public static string Build(string transactionToken, decimal amount, string currency = "EUR") =>
        $"ideal://pay?tr={Uri.EscapeDataString(transactionToken)}&amount={amount:F2}&currency={Uri.EscapeDataString(currency)}";

    public static string BuildEmvQr(string transactionToken, decimal amount, string merchantName, string currency = "EUR")
    {
        var payload = $"00020101021226580014ideal.nl0113{transactionToken}52045{merchantName[..Math.Min(25, merchantName.Length)]}5303{currency}540{amount:F2}5802NL6304";
        return payload;
    }
}
