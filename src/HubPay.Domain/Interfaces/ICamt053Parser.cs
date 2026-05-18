namespace HubPay.Domain.Interfaces;

public interface ICamt053Parser
{
    IReadOnlyList<Camt053Entry> ParseEntries(string xmlContent);
}
