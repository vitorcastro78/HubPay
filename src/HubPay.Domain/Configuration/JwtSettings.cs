namespace HubPay.Domain.Configuration;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "https://hubpay.eu";
    public string Audience { get; set; } = "hubpay-api";
    public string SecretKey { get; set; } = "DEV_ONLY_CHANGE_IN_PRODUCTION_MIN_32_CHARS!!";
    public int ExpirationMinutes { get; set; } = 60;
}
