using HubPay.Application.Commands;
using HubPay.Domain.Enums;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using MediatR;

namespace HubPay.Application.Handlers;

public sealed class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, RefundPaymentResult>
{
    private readonly ITransactionRepository _repository;
    private readonly IPaymentStrategyFactory _strategyFactory;

    public RefundPaymentCommandHandler(
        ITransactionRepository repository,
        IPaymentStrategyFactory strategyFactory)
    {
        _repository = repository;
        _strategyFactory = strategyFactory;
    }

    public async Task<RefundPaymentResult> Handle(RefundPaymentCommand command, CancellationToken ct)
    {
        var transaction = await _repository.GetByIdAsync(command.TransactionId, ct)
            ?? throw new BusinessRuleException("Transação não encontrada.");

        if (transaction.Status is not (TransactionStatus.Authorized or TransactionStatus.Settled))
            throw new BusinessRuleException("Reembolso apenas permitido para Authorized ou Settled.");

        var amount = command.Amount ?? transaction.Amount;
        var strategy = _strategyFactory.Resolve(transaction.PaymentScheme);
        var result = await strategy.RefundAsync(transaction.Id, amount, ct);

        if (result.Success)
        {
            transaction.Refund();
            await _repository.UpdateAsync(transaction, ct);
        }

        return new RefundPaymentResult(result.Success, result.RefundReference, result.Status);
    }
}
