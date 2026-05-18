using HubPay.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace HubPay.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var problem = new ValidationProblemDetails(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Erro de validação",
                Type = "https://tools.ietf.org/html/rfc7807"
            };
            await WriteProblemAsync(context, problem);
        }
        catch (BusinessRuleException ex)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Regra de negócio violada",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            };
            await WriteProblemAsync(context, problem);
        }
        catch (PspIntegrationException ex)
        {
            var problem = new ProblemDetails
            {
                Status = ex.HttpStatusCode ?? StatusCodes.Status502BadGateway,
                Title = $"Falha na integração PSP ({ex.Scheme})",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            };
            await WriteProblemAsync(context, problem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno do servidor",
                Detail = "Ocorreu um erro inesperado.",
                Type = "https://tools.ietf.org/html/rfc7807"
            };
            await WriteProblemAsync(context, problem);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
