using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;

namespace HubPay.Infrastructure.Configuration;

public sealed class HubPaySettingsProvider : IHubPaySettingsProvider
{
    private HubPaySettings _current = new();

    public HubPaySettings Current => _current;
    public bool IsInitialized { get; private set; }

    public void Initialize(HubPaySettings settings)
    {
        _current = settings;
        IsInitialized = true;
    }
}
