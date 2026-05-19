using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HubPay.WebApi.OpenApi;

/// <summary>Documents the mandatory idempotency header on POST /api/v1/payments.</summary>
internal sealed class PaymentIdempotencyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.RelativePath, "api/v1/payments", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(context.ApiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Unique idempotency key for this payment attempt.",
            Schema = new OpenApiSchema { Type = "string", MinLength = 8, MaxLength = 128 },
            Example = new Microsoft.OpenApi.Any.OpenApiString("pay-7f3c9a2b-4d1e-4f88-9c0a-1b2c3d4e5f6a")
        });

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "IdempotencyKey" }
                },
                Array.Empty<string>()
            }
        });
    }
}
