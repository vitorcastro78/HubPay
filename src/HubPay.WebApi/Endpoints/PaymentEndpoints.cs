using HubPay.Application.Commands;
using HubPay.Application.DTOs;
using HubPay.Application.Queries;
using HubPay.Domain.Configuration;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.Telemetry;
using HubPay.WebApi.OpenApi;
using MediatR;
using Microsoft.Extensions.Options;

namespace HubPay.WebApi.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1")
            .WithTags(HubPayApiDescriptions.TagPayments)
            .RequireAuthorization();

        api.MapPost("/payments", async (CreatePaymentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreatePaymentCommand(request), ct);
            HubPayMetrics.PaymentsCreated.Add(1);
            return Results.Ok(result);
        })
        .WithName("CreatePayment")
        .WithSummary(HubPayApiDescriptions.CreatePaymentSummary)
        .WithDescription(HubPayApiDescriptions.CreatePaymentDescription)
        .Produces<PaymentResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapGet("/transactions", async (int page, int pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTransactionsQuery(page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetTransactions")
        .WithSummary(HubPayApiDescriptions.GetTransactionsSummary)
        .WithDescription(HubPayApiDescriptions.GetTransactionsDescription)
        .Produces<PagedResult<TransactionDto>>(StatusCodes.Status200OK);

        api.MapGet("/dashboard/stats", async (IMediator mediator, CancellationToken ct) =>
        {
            var stats = await mediator.Send(new GetDashboardStatsQuery(), ct);
            return Results.Ok(stats);
        })
        .WithName("GetDashboardStats")
        .WithSummary(HubPayApiDescriptions.DashboardStatsSummary)
        .WithDescription(HubPayApiDescriptions.DashboardStatsDescription)
        .Produces<DashboardStatsDto>(StatusCodes.Status200OK);

        api.MapGet("/transactions/{id:guid}/antifraud", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var detail = await mediator.Send(new GetAntiFraudDetailQuery(id), ct);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        })
        .WithName("GetAntiFraudDetail")
        .WithSummary(HubPayApiDescriptions.AntiFraudDetailSummary)
        .WithDescription(HubPayApiDescriptions.AntiFraudDetailDescription)
        .Produces<AntiFraudDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/transactions/{id:guid}/refund", async (Guid id, decimal? amount, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RefundPaymentCommand(id, amount), ct);
            return Results.Ok(result);
        })
        .WithName("RefundPayment")
        .WithSummary(HubPayApiDescriptions.RefundSummary)
        .WithDescription(HubPayApiDescriptions.RefundDescription)
        .Produces<RefundPaymentResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/api/v1/webhooks/{scheme}", async (
            string scheme,
            HttpRequest request,
            IPaymentStrategyFactory factory,
            IWebhookSignatureValidator signatureValidator,
            IOptions<HubPaySettings> options,
            CancellationToken ct) =>
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var payload = await reader.ReadToEndAsync(ct);
            request.Body.Position = 0;

            var headerName = options.Value.Webhooks.SignatureHeaderName;
            var signature = request.Headers[headerName].FirstOrDefault();

            if (!signatureValidator.Validate(scheme, payload, signature))
            {
                return Results.Problem(
                    title: "Invalid webhook signature",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var strategy = factory.Resolve(scheme);
            var result = await strategy.HandleWebhookAsync(payload, headers, ct);
            return Results.Ok(result);
        })
        .WithTags(HubPayApiDescriptions.TagPayments)
        .WithName("PaymentWebhook")
        .WithSummary(HubPayApiDescriptions.WebhookSummary)
        .WithDescription(HubPayApiDescriptions.WebhookDescription)
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .AllowAnonymous();
    }
}
