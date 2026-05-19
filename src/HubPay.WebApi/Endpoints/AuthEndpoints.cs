using HubPay.Domain.Configuration;
using HubPay.WebApi.Auth;
using HubPay.WebApi.OpenApi;
using Microsoft.Extensions.Options;

namespace HubPay.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/v1/auth")
            .WithTags(HubPayApiDescriptions.TagAuthentication);

        auth.MapPost("/token", (TokenRequest request, IOptions<HubPaySettings> options) =>
        {
            if (string.IsNullOrWhiteSpace(request.MerchantId))
                return Results.BadRequest(new { detail = "MerchantId is required." });

            var jwt = options.Value.Jwt;
            var token = JwtExtensions.GenerateToken(jwt, request.MerchantId, request.Role ?? "merchant");
            return Results.Ok(new TokenResponse(token, DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes)));
        })
        .WithName("GetDevToken")
        .WithSummary(HubPayApiDescriptions.AuthTokenSummary)
        .WithDescription(HubPayApiDescriptions.AuthTokenDescription)
        .Produces<TokenResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }

    /// <summary>JWT token request body.</summary>
    /// <param name="MerchantId">Merchant identifier (max 64 characters).</param>
    /// <param name="Role">Optional: <c>merchant</c> (default) or <c>admin</c> for PSP configuration API.</param>
    public sealed record TokenRequest(string MerchantId, string? Role);

    /// <summary>JWT token response.</summary>
    /// <param name="AccessToken">Bearer token value.</param>
    /// <param name="ExpiresAt">UTC expiration timestamp.</param>
    public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt);
}
