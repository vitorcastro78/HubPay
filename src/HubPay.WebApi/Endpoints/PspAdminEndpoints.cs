using HubPay.Application.DTOs.Admin;
using HubPay.Application.Interfaces;
using HubPay.WebApi.OpenApi;

namespace HubPay.WebApi.Endpoints;

public static class PspAdminEndpoints
{
    public static void MapPspAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/psp-config")
            .WithTags(HubPayApiDescriptions.TagPspAdmin)
            .RequireAuthorization("Admin");

        admin.MapGet("/providers", async (IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var items = await service.ListProvidersAsync(ct);
            return Results.Ok(items);
        })
        .WithName("ListPspProviders")
        .WithSummary(HubPayApiDescriptions.ListProvidersSummary)
        .WithDescription(HubPayApiDescriptions.ListProvidersDescription)
        .Produces<IReadOnlyList<PspProviderConfigDto>>(StatusCodes.Status200OK);

        admin.MapGet("/providers/{scheme}", async (string scheme, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var item = await service.GetProviderAsync(scheme, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetPspProvider")
        .WithSummary(HubPayApiDescriptions.GetProviderSummary)
        .WithDescription(HubPayApiDescriptions.GetProviderDescription)
        .Produces<PspProviderConfigDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        admin.MapPost("/providers", async (CreatePspProviderRequest request, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var created = await service.CreateProviderAsync(request, ct);
            return Results.Created($"/api/v1/admin/psp-config/providers/{created.Scheme}", created);
        })
        .WithName("CreatePspProvider")
        .WithSummary(HubPayApiDescriptions.CreateProviderSummary)
        .WithDescription(HubPayApiDescriptions.CreateProviderDescription)
        .Produces<PspProviderConfigDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPut("/providers/{scheme}", async (
            string scheme,
            UpdatePspProviderRequest request,
            IPspConfigurationAdminService service,
            CancellationToken ct) =>
        {
            var updated = await service.UpdateProviderAsync(scheme, request, ct);
            return Results.Ok(updated);
        })
        .WithName("UpdatePspProvider")
        .WithSummary(HubPayApiDescriptions.UpdateProviderSummary)
        .WithDescription(HubPayApiDescriptions.UpdateProviderDescription)
        .Produces<PspProviderConfigDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        admin.MapDelete("/providers/{scheme}", async (string scheme, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            await service.DeleteProviderAsync(scheme, ct);
            return Results.NoContent();
        })
        .WithName("DeletePspProvider")
        .WithSummary(HubPayApiDescriptions.DeleteProviderSummary)
        .WithDescription(HubPayApiDescriptions.DeleteProviderDescription)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        admin.MapGet("/merchants", async (string? scheme, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var items = await service.ListMerchantsAsync(scheme, ct);
            return Results.Ok(items);
        })
        .WithName("ListPspMerchants")
        .WithSummary(HubPayApiDescriptions.ListMerchantsSummary)
        .WithDescription(HubPayApiDescriptions.ListMerchantsDescription)
        .Produces<IReadOnlyList<PspMerchantConfigDto>>(StatusCodes.Status200OK);

        admin.MapGet("/merchants/{scheme}/{merchantId}", async (
            string scheme,
            string merchantId,
            IPspConfigurationAdminService service,
            CancellationToken ct) =>
        {
            var item = await service.GetMerchantAsync(scheme, merchantId, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetPspMerchant")
        .WithSummary(HubPayApiDescriptions.GetMerchantSummary)
        .WithDescription(HubPayApiDescriptions.GetMerchantDescription)
        .Produces<PspMerchantConfigDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        admin.MapPost("/merchants", async (UpsertPspMerchantRequest request, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var created = await service.UpsertMerchantAsync(request, ct);
            return Results.Created(
                $"/api/v1/admin/psp-config/merchants/{created.Scheme}/{created.MerchantId}",
                created);
        })
        .WithName("CreatePspMerchant")
        .WithSummary(HubPayApiDescriptions.UpsertMerchantSummary)
        .WithDescription(HubPayApiDescriptions.UpsertMerchantDescription)
        .Produces<PspMerchantConfigDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        admin.MapPut("/merchants/{scheme}/{merchantId}", async (
            string scheme,
            string merchantId,
            UpsertPspMerchantRequest request,
            IPspConfigurationAdminService service,
            CancellationToken ct) =>
        {
            if (!string.Equals(request.Scheme, scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(request.MerchantId, merchantId, StringComparison.Ordinal))
            {
                return Results.BadRequest(new { detail = "Scheme and MerchantId in the body must match the route." });
            }

            var updated = await service.UpsertMerchantAsync(request, ct);
            return Results.Ok(updated);
        })
        .WithName("UpdatePspMerchant")
        .WithSummary(HubPayApiDescriptions.UpdateMerchantSummary)
        .WithDescription(HubPayApiDescriptions.UpdateMerchantDescription)
        .Produces<PspMerchantConfigDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        admin.MapDelete("/merchants/{scheme}/{merchantId}", async (
            string scheme,
            string merchantId,
            IPspConfigurationAdminService service,
            CancellationToken ct) =>
        {
            await service.DeleteMerchantAsync(scheme, merchantId, ct);
            return Results.NoContent();
        })
        .WithName("DeletePspMerchant")
        .WithSummary(HubPayApiDescriptions.DeleteMerchantSummary)
        .WithDescription(HubPayApiDescriptions.DeleteMerchantDescription)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        admin.MapPost("/reload", async (IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var result = await service.ReloadAsync(ct);
            return Results.Ok(result);
        })
        .WithName("ReloadPspConfiguration")
        .WithSummary(HubPayApiDescriptions.ReloadSummary)
        .WithDescription(HubPayApiDescriptions.ReloadDescription)
        .Produces<PspConfigurationReloadResult>(StatusCodes.Status200OK);
    }
}
