using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;

namespace HubPay.Infrastructure.Payments;

public sealed class PaymentStrategyFactory : IPaymentStrategyFactory
{
    private readonly IEnumerable<IPaymentStrategy> _strategies;

    public PaymentStrategyFactory(IEnumerable<IPaymentStrategy> strategies) => _strategies = strategies;

    public IPaymentStrategy Resolve(string paymentScheme)
    {
        var scheme = paymentScheme.ToUpperInvariant();
        var strategy = _strategies.FirstOrDefault(s =>
            string.Equals(s.SchemeName, scheme, StringComparison.OrdinalIgnoreCase));

        return strategy ?? throw new BusinessRuleException($"Esquema de pagamento não suportado: {paymentScheme}");
    }
}
