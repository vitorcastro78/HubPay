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

    private static readonly string[] PhoneRequiredSchemes = ["MBWAY", "BIZUM", "BANCOMATPAY"];

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

        RuleFor(x => x.Request.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("Telefone E.164 obrigatório (ex: +351912345678).")
            .When(x => PhoneRequiredSchemes.Contains(x.Request.PaymentScheme.ToUpperInvariant()));
    }
}
