namespace HubPay.Domain.Configuration;

public sealed class Camt053Settings
{
    public string InboundDirectory { get; set; } = "data/camt053/inbound";
    public string ProcessedDirectory { get; set; } = "data/camt053/processed";
    public bool UseSimulatedStatementsWhenEmpty { get; set; } = true;
}
