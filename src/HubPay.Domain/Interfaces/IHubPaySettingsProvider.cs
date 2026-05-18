using HubPay.Domain.Configuration;

namespace HubPay.Domain.Interfaces;

public interface IHubPaySettingsProvider
{
    HubPaySettings Current { get; }
    bool IsInitialized { get; }
    void Initialize(HubPaySettings settings);
}
