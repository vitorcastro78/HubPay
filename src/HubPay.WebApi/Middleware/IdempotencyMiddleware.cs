using System.Text;
using System.Text.Json;
using HubPay.Domain.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HubPay.WebApi.Middleware;

public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "X-Idempotency-Key";
    private const string InFlight = "IN_FLIGHT";
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly HubPaySettings _settings;

    public IdempotencyMiddleware(RequestDelegate next, IConnectionMultiplexer redis, IOptions<HubPaySettings> options)
    {
        _next = next;
        _redis = redis;
        _settings = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Path.StartsWithSegments("/api/v1/payments"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Cabeçalho obrigatório em falta",
                detail = $"O header {HeaderName} é obrigatório para operações mutáveis."
            });
            return;
        }

        var idempotencyKey = keyValues.ToString();
        var redisKey = $"hubpay:idempotency:{idempotencyKey}";
        var db = _redis.GetDatabase();
        var ttl = TimeSpan.FromHours(_settings.IdempotencyTtlHours);

        var existing = await db.StringGetAsync(redisKey);
        if (existing.HasValue)
        {
            var state = existing.ToString();
            if (state == InFlight)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Conflito de idempotência",
                    detail = "Pedido idêntico ainda em processamento."
                });
                return;
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(state);
            return;
        }

        await db.StringSetAsync(redisKey, InFlight, ttl);

        var originalBody = context.Response.Body;
        await using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            memoryStream.Position = 0;
            var responseBody = await new StreamReader(memoryStream, Encoding.UTF8).ReadToEndAsync();

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await db.StringSetAsync(redisKey, responseBody, ttl);
            }
            else
            {
                await db.KeyDeleteAsync(redisKey);
            }

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBody);
        }
        catch
        {
            await db.KeyDeleteAsync(redisKey);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
