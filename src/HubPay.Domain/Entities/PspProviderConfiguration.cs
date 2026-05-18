namespace HubPay.Domain.Entities;

public sealed class PspProviderConfiguration
{
    public Guid Id { get; set; }
    public string Scheme { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string SettingsJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
