using HubPay.Application.Commands;
using HubPay.Application.DTOs;
using HubPay.Application.Queries;
using HubPay.Domain.Interfaces;
using MediatR;

namespace HubPay.WebApi.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").WithTags("HubPay");

        api.MapPost("/payments", async (CreatePaymentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreatePaymentCommand(request), ct);
            return Results.Ok(result);
        })
        .WithName("CreatePayment")
        .Produces<PaymentResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapGet("/transactions", async (int page, int pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTransactionsQuery(page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetTransactions");

        api.MapGet("/dashboard/stats", async (IMediator mediator, CancellationToken ct) =>
        {
            var stats = await mediator.Send(new GetDashboardStatsQuery(), ct);
            return Results.Ok(stats);
        })
        .WithName("GetDashboardStats");

        api.MapGet("/transactions/{id:guid}/antifraud", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var detail = await mediator.Send(new GetAntiFraudDetailQuery(id), ct);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        })
        .WithName("GetAntiFraudDetail");

        api.MapPost("/transactions/{id:guid}/refund", async (Guid id, decimal? amount, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RefundPaymentCommand(id, amount), ct);
            return Results.Ok(result);
        })
        .WithName("RefundPayment");

        api.MapPost("/webhooks/{scheme}", async (
            string scheme,
            HttpRequest request,
            IPaymentStrategyFactory factory,
            CancellationToken ct) =>
        {
            using var reader = new StreamReader(request.Body);
            var payload = await reader.ReadToEndAsync(ct);
            var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var strategy = factory.Resolve(scheme);
            var result = await strategy.HandleWebhookAsync(payload, headers, ct);
            return Results.Ok(result);
        })
        .WithName("PaymentWebhook");
    }
}
