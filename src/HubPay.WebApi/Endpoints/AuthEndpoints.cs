using HubPay.Domain.Configuration;
using HubPay.WebApi.Auth;
using Microsoft.Extensions.Options;

namespace HubPay.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        auth.MapPost("/token", (TokenRequest request, IOptions<HubPaySettings> options) =>
        {
            if (string.IsNullOrWhiteSpace(request.MerchantId))
                return Results.BadRequest(new { detail = "MerchantId é obrigatório." });

            var jwt = options.Value.Jwt;
            var token = JwtExtensions.GenerateToken(jwt, request.MerchantId, request.Role ?? "merchant");
            return Results.Ok(new TokenResponse(token, DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes)));
        })
        .WithName("GetDevToken")
        .AllowAnonymous();
    }

    public sealed record TokenRequest(string MerchantId, string? Role);
    public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt);
}
