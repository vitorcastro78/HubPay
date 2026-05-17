namespace HubPay.Domain.Interfaces;

public interface IPaymentStrategyFactory
{
    IPaymentStrategy Resolve(string paymentScheme);
}
