using HubPay.Application.Queries;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HubPay.WebApi.OpenApi;

/// <summary>Enriches OpenAPI operations for minimal API endpoints (parameters, bodies).</summary>
internal sealed class HubPayEndpointOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointName = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointNameMetadata>()
            .FirstOrDefault()?.EndpointName;

        if (endpointName is null)
            return;

        switch (endpointName)
        {
            case "GetTransactions":
                AddQueryParam(operation, "page", "Page number (1-based).", "integer", "1");
                AddQueryParam(operation, "pageSize", "Page size (default 20, max 100).", "integer", "20");
                break;
            case "GetAntiFraudDetail":
                AddPathParam(operation, "id", "Transaction unique identifier (GUID).", format: "uuid");
                break;
            case "RefundPayment":
                AddPathParam(operation, "id", "Transaction unique identifier (GUID).", format: "uuid");
                AddQueryParam(operation, "amount", "Partial refund amount in EUR; omit for full refund.", "number", null, required: false);
                break;
            case "PaymentWebhook":
                AddPathParam(operation, "scheme",
                    "Payment scheme. Allowed: " + string.Join(", ", HubPayApiDescriptions.PaymentSchemes),
                    enumValues: HubPayApiDescriptions.PaymentSchemes);
                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Description = "Raw PSP callback payload (JSON or form-encoded depending on provider).",
                    Content =
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Type = "object", AdditionalPropertiesAllowed = true }
                        }
                    }
                };
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-HubPay-Signature",
                    In = ParameterLocation.Header,
                    Required = false,
                    Description = "HMAC signature (header name configurable via WEBHOOKS settings).",
                    Schema = new OpenApiSchema { Type = "string" }
                });
                operation.Security = [];
                break;
            case "GetPspProvider":
            case "UpdatePspProvider":
                AddSchemePathParam(operation);
                break;
            case "DeletePspProvider":
                AddSchemePathParam(operation, HubPayApiDescriptions.ProviderSchemes);
                break;
            case "ListPspMerchants":
                AddQueryParam(operation, "scheme", "Filter by provider scheme (e.g. SIBS, WERO).", "string", null, required: false);
                break;
            case "GetPspMerchant":
            case "UpdatePspMerchant":
            case "DeletePspMerchant":
                AddSchemePathParam(operation);
                AddPathParam(operation, "merchantId", "Merchant identifier (max 64 characters).");
                break;
        }
    }

    private static void AddPathParam(
        OpenApiOperation op,
        string name,
        string description,
        string? format = null,
        IEnumerable<string>? enumValues = null)
    {
        op.Parameters ??= [];
        var schema = new OpenApiSchema { Type = "string", Description = description, Format = format };
        if (enumValues is not null)
        {
            schema.Enum = enumValues
                .Select(v => new Microsoft.OpenApi.Any.OpenApiString(v) as Microsoft.OpenApi.Any.IOpenApiAny)
                .ToList();
        }

        op.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Path,
            Required = true,
            Description = description,
            Schema = schema
        });
    }

    private static void AddSchemePathParam(OpenApiOperation op, IEnumerable<string>? enumValues = null) =>
        AddPathParam(op, "scheme",
            "Provider configuration key (not payment scheme). Examples: SIBS, BIZUM, WEBHOOKS.",
            enumValues: enumValues);

    private static void AddQueryParam(
        OpenApiOperation op,
        string name,
        string description,
        string type,
        string? example,
        bool required = true)
    {
        op.Parameters ??= [];
        var param = new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Query,
            Required = required,
            Description = description,
            Schema = new OpenApiSchema { Type = type }
        };
        if (example is not null)
            param.Schema.Example = new Microsoft.OpenApi.Any.OpenApiString(example);
        op.Parameters.Add(param);
    }
}
