using FluentValidation;
using HubPay.Application.Behaviors;
using HubPay.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace HubPay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<CreatePaymentCommandValidator>();
        return services;
    }
}
