using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HubPay.WebApi.OpenApi;

internal sealed class OpenApiTagsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags =
        [
            new OpenApiTag
            {
                Name = HubPayApiDescriptions.TagAuthentication,
                Description = HubPayApiDescriptions.TagAuthenticationDesc
            },
            new OpenApiTag
            {
                Name = HubPayApiDescriptions.TagPayments,
                Description = HubPayApiDescriptions.TagPaymentsDesc
            },
            new OpenApiTag
            {
                Name = HubPayApiDescriptions.TagPspAdmin,
                Description = HubPayApiDescriptions.TagPspAdminDesc
            },
            new OpenApiTag
            {
                Name = HubPayApiDescriptions.TagHealth,
                Description = HubPayApiDescriptions.TagHealthDesc
            }
        ];
    }
}
