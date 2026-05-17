using FluentValidation;
using HubPay.Application.Commands;

namespace HubPay.Application.Validators;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    private static readonly string[] AllowedSchemes =
    [
        "MBWAY", "MULTIBANCO", "BIZUM", "EURO6000", "WERO", "CARTESBANCAIRES",
        "IDEAL", "BANCONTACT", "BANCOMATPAY", "SWISH", "VIPPSMOBILEPAY"
    ];

    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.Request.MerchantId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Currency).Equal("EUR").WithMessage("A moeda deve ser estritamente EUR.");
        RuleFor(x => x.Request.PaymentScheme)
            .NotEmpty()
            .Must(s => AllowedSchemes.Contains(s.ToUpperInvariant()))
            .WithMessage("Esquema de pagamento não suportado.");
        RuleFor(x => x.Request.EndToEndId).NotEmpty().MaximumLength(35);
        RuleFor(x => x.Request.CustomerIP).NotEmpty();
        RuleFor(x => x.Request.DeviceFingerprint).NotEmpty();
        RuleFor(x => x.Request.CustomerEmail).NotEmpty().EmailAddress();
    }
}
