using HubPay.Application.DTOs;
using HubPay.Application.DTOs.Admin;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HubPay.WebApi.OpenApi;

internal sealed class OpenApiSchemaExamplesFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreatePaymentRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["merchantId"] = new OpenApiString("merchant-demo-001"),
                ["amount"] = new OpenApiDouble(49.99),
                ["currency"] = new OpenApiString("EUR"),
                ["paymentScheme"] = new OpenApiString("MBWAY"),
                ["endToEndId"] = new OpenApiString("E2E-20260518-0001"),
                ["customerIP"] = new OpenApiString("203.0.113.10"),
                ["deviceFingerprint"] = new OpenApiString("fp-a1b2c3d4-e5f6"),
                ["customerEmail"] = new OpenApiString("buyer@example.com"),
                ["countryCode"] = new OpenApiString("PT"),
                ["phoneNumber"] = new OpenApiString("+351912345678")
            };
        }
        else if (context.Type.Name == "TokenRequest")
        {
            schema.Example = new OpenApiObject
            {
                ["merchantId"] = new OpenApiString("merchant-demo-001"),
                ["role"] = new OpenApiString("merchant")
            };
        }
        else if (context.Type == typeof(CreatePspProviderRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["scheme"] = new OpenApiString("SIBS"),
                ["isEnabled"] = new OpenApiBoolean(true),
                ["settingsJson"] = new OpenApiString("""{"baseUrl":"https://api.sibs.pt","enableSimulationFallback":false}""")
            };
        }
        else if (context.Type == typeof(UpsertPspMerchantRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["scheme"] = new OpenApiString("SIBS"),
                ["merchantId"] = new OpenApiString("merchant-demo-001"),
                ["settingsJson"] = new OpenApiString("""{"multibancoEntity":"12345"}""")
            };
        }
    }
}
