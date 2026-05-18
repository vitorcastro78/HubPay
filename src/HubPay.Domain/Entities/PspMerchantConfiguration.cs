namespace HubPay.Domain.Entities;

public sealed class PspMerchantConfiguration
{
    public Guid Id { get; set; }
    public string Scheme { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
