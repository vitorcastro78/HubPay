using HubPay.Application.Commands;
using HubPay.Application.DTOs;
using HubPay.Domain.Entities;
using HubPay.Domain.Enums;
using HubPay.Domain.Exceptions;
using HubPay.Domain.Interfaces;
using MediatR;

namespace HubPay.Application.Handlers;

public sealed class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentResponseDto>
{
    private readonly ITransactionRepository _repository;
    private readonly IAntiFraudEngine _antiFraudEngine;
    private readonly IPaymentStrategyFactory _strategyFactory;

    public CreatePaymentCommandHandler(
        ITransactionRepository repository,
        IAntiFraudEngine antiFraudEngine,
        IPaymentStrategyFactory strategyFactory)
    {
        _repository = repository;
        _antiFraudEngine = antiFraudEngine;
        _strategyFactory = strategyFactory;
    }

    public async Task<PaymentResponseDto> Handle(CreatePaymentCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var transaction = Transaction.Create(
            req.MerchantId,
            req.Amount,
            req.Currency,
            req.PaymentScheme.ToUpperInvariant(),
            req.EndToEndId,
            req.CustomerIP,
            req.DeviceFingerprint,
            req.CustomerEmail,
            req.CountryCode);

        await _repository.AddAsync(transaction, ct);

        transaction.StartAntiFraudEvaluation();
        await _repository.UpdateAsync(transaction, ct);

        var fraudResult = await _antiFraudEngine.EvaluateAsync(transaction, ct);

        try
        {
            transaction.ApplyAntiFraudResult(fraudResult.Score, fraudResult.ScaStatus, fraudResult.ElapsedMilliseconds);
        }
        catch (BusinessRuleException)
        {
            await _repository.UpdateAsync(transaction, ct);
            throw;
        }

        await _repository.UpdateAsync(transaction, ct);

        object? challengePayload = null;
        string? redirectUrl = null;
        string? externalRef = null;

        if (transaction.Status == TransactionStatus.Authorized ||
            transaction.Status == TransactionStatus.Pending)
        {
            var strategy = _strategyFactory.Resolve(transaction.PaymentScheme);
            var paymentResult = await strategy.ProcessAsync(transaction, ct);

            if (paymentResult.Success)
            {
                if (transaction.Status == TransactionStatus.Authorized)
                    transaction.MarkPending(paymentResult.ExternalReference);
                else
                    transaction.MarkPending(paymentResult.ExternalReference);

                if (fraudResult.ScaStatus != "TRA" && transaction.Status == TransactionStatus.Pending)
                {
                    challengePayload = new
                    {
                        type = "3DS_CHALLENGE",
                        acsUrl = paymentResult.RedirectUrl ?? "https://acs.hubpay.eu/challenge",
                        transactionId = transaction.Id
                    };
                }
                else if (transaction.Status == TransactionStatus.Pending && fraudResult.ScaStatus == "TRA")
                {
                    transaction.Authorize(paymentResult.ExternalReference);
                }

                redirectUrl = paymentResult.RedirectUrl;
                externalRef = paymentResult.ExternalReference;
                await _repository.UpdateAsync(transaction, ct);
            }
        }

        return new PaymentResponseDto(
            transaction.Id,
            transaction.Status.ToString(),
            transaction.ScaStatus,
            transaction.AntiFraudScore,
            redirectUrl,
            externalRef,
            challengePayload,
            transaction.AntiFraudElapsedMs);
    }
}
