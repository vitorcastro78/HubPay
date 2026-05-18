using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HubPay.Domain.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace HubPay.WebApi.Auth;

public static class JwtExtensions
{
    public static IServiceCollection AddHubPayAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()?.Jwt ?? new JwtSettings();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static string GenerateToken(JwtSettings jwt, string merchantId, string role = "merchant")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, merchantId),
            new Claim(ClaimTypes.Role, role),
            new Claim("merchant_id", merchantId)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static IServiceCollection ReplaceTransactionNotifier<T>(this IServiceCollection services)
        where T : class, HubPay.Domain.Interfaces.ITransactionNotifier
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(HubPay.Domain.Interfaces.ITransactionNotifier));
        if (descriptor is not null)
            services.Remove(descriptor);
        services.AddScoped<HubPay.Domain.Interfaces.ITransactionNotifier, T>();
        return services;
    }
}
