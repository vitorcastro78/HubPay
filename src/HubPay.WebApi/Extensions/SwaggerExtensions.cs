using System.Reflection;
using HubPay.WebApi.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HubPay.WebApi.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddHubPaySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(HubPayApiDescriptions.ApiVersion, new OpenApiInfo
            {
                Title = HubPayApiDescriptions.ApiTitle,
                Version = HubPayApiDescriptions.ApiVersion,
                Description = HubPayApiDescriptions.ApiDescription,
                Contact = new OpenApiContact
                {
                    Name = "HubPay Engineering",
                    Url = new Uri("https://github.com/vitorcastro78/HubPay")
                },
                License = new OpenApiLicense
                {
                    Name = "Proprietary",
                    Url = new Uri("https://hubpay.eu")
                }
            });

            options.DocumentFilter<OpenApiTagsDocumentFilter>();
            options.SchemaFilter<OpenApiSchemaExamplesFilter>();

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "JWT obtained from **POST /api/v1/auth/token**. Example: `Authorization: Bearer eyJhbG...`"
            });

            options.AddSecurityDefinition("IdempotencyKey", new OpenApiSecurityScheme
            {
                Name = "X-Idempotency-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Required for **POST /api/v1/payments**. Unique key per payment attempt (24h TTL)."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<PaymentIdempotencyOperationFilter>();
            options.OperationFilter<HubPayEndpointOperationFilter>();

            IncludeXmlComments(options, Assembly.GetExecutingAssembly());

            var applicationAssembly = typeof(HubPay.Application.DTOs.CreatePaymentRequest).Assembly;
            IncludeXmlComments(options, applicationAssembly);
        });

        return services;
    }

    public static WebApplication UseHubPaySwagger(this WebApplication app)
    {
        var enabled = app.Configuration.GetValue("HubPay:EnableSwagger", app.Environment.IsDevelopment());
        if (!enabled)
            return app;

        app.UseSwagger(options =>
        {
            options.RouteTemplate = "openapi/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/openapi/{HubPayApiDescriptions.ApiVersion}/swagger.json",
                $"{HubPayApiDescriptions.ApiTitle} {HubPayApiDescriptions.ApiVersion}");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = $"{HubPayApiDescriptions.ApiTitle} — Swagger UI";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
        });

        app.UseReDoc(options =>
        {
            options.RoutePrefix = "redoc";
            options.DocumentTitle = $"{HubPayApiDescriptions.ApiTitle} — API Reference";
            options.SpecUrl = $"/openapi/{HubPayApiDescriptions.ApiVersion}/swagger.json";
            options.EnableUntrustedSpec();
            options.ScrollYOffset(0);
            options.RequiredPropsFirst();
            options.SortPropsAlphabetically();
            options.HideHostname();
            options.ExpandResponses("200,201");
            options.PathInMiddlePanel();
            options.NativeScrollbars();
        });

        app.MapGet("/docs", () => Results.Redirect("/redoc"))
            .ExcludeFromDescription()
            .AllowAnonymous();

        return app;
    }

    private static void IncludeXmlComments(SwaggerGenOptions options, Assembly assembly)
    {
        var xmlFile = $"{assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
}
