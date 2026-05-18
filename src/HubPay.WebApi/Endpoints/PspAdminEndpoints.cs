using HubPay.Application.DTOs.Admin;
using HubPay.Application.Interfaces;

namespace HubPay.WebApi.Endpoints;

public static class PspAdminEndpoints
{
    public static void MapPspAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/psp-config")
            .WithTags("PSP Admin")
            .RequireAuthorization("Admin");

        admin.MapGet("/providers", async (IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var items = await service.ListProvidersAsync(ct);
            return Results.Ok(items);
        })
        .WithName("ListPspProviders");

        admin.MapGet("/providers/{scheme}", async (string scheme, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var item = await service.GetProviderAsync(scheme, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetPspProvider");

        admin.MapPost("/providers", async (CreatePspProviderRequest request, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var created = await service.CreateProviderAsync(request, ct);
            return Results.Created($"/api/v1/admin/psp-config/providers/{created.Scheme}", created);
        })
        .WithName("CreatePspProvider");

        admin.MapPut("/providers/{scheme}", async (
            string scheme,
            UpdatePspProviderRequest request,
            IPspConfigurationAdminService service,
            CancellationToken ct) =>
        {
            var updated = await service.UpdateProviderAsync(scheme, request, ct);
            return Results.Ok(updated);
        })
        .WithName("UpdatePspProvider");

        admin.MapDelete("/providers/{scheme}", async (string scheme, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            await service.DeleteProviderAsync(scheme, ct);
            return Results.NoContent();
        })
        .WithName("DeletePspProvider");

        admin.MapGet("/merchants", async (string? scheme, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var items = await service.ListMerchantsAsync(scheme, ct);
            return Results.Ok(items);
        })
        .WithName("ListPspMerchants");

        admin.MapGet("/merchants/{scheme}/{merchantId}", async (
            string scheme,
            string merchantId,
            IPspConfigurationAdminService service,
            CancellationToken ct) =>
        {
            var item = await service.GetMerchantAsync(scheme, merchantId, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetPspMerchant");

        admin.MapPost("/merchants", async (UpsertPspMerchantRequest request, IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var created = await service.UpsertMerchantAsync(request, ct);
            return Results.Created(
                $"/api/v1/admin/psp-config/merchants/{created.Scheme}/{created.MerchantId}",
                created);
        })
        .WithName("CreatePspMerchant");

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
                return Results.BadRequest(new { detail = "Scheme e MerchantId do corpo devem coincidir com a rota." });
            }

            var updated = await service.UpsertMerchantAsync(request, ct);
            return Results.Ok(updated);
        })
        .WithName("UpdatePspMerchant");

        admin.MapDelete("/merchants/{scheme}/{merchantId}", async (
            string scheme,
            string merchantId,
            IPspConfigurationAdminService service,
            CancellationToken ct) =>
        {
            await service.DeleteMerchantAsync(scheme, merchantId, ct);
            return Results.NoContent();
        })
        .WithName("DeletePspMerchant");

        admin.MapPost("/reload", async (IPspConfigurationAdminService service, CancellationToken ct) =>
        {
            var result = await service.ReloadAsync(ct);
            return Results.Ok(result);
        })
        .WithName("ReloadPspConfiguration");
    }
}
