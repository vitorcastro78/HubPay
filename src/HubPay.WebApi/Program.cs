using FluentValidation;
using Microsoft.EntityFrameworkCore;
using HubPay.Application;
using HubPay.Domain.Configuration;
using HubPay.Infrastructure;
using HubPay.Domain.Interfaces;
using HubPay.Infrastructure.Persistence;
using HubPay.WebApi.Auth;
using HubPay.WebApi.Endpoints;
using HubPay.WebApi.Extensions;
using HubPay.WebApi.Hubs;
using HubPay.WebApi.Middleware;
using HubPay.WebApi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.Configure<HubPaySettings>(builder.Configuration.GetSection(HubPaySettings.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ReplaceTransactionNotifier<TransactionSignalRNotifier>();
builder.Services.AddValidatorsFromAssemblyContaining<HubPay.Application.Validators.CreatePaymentCommandValidator>();

builder.Services.AddHubPayAuthentication(builder.Configuration);
builder.Services.AddHubPayObservability();
builder.Services.AddHubPayHealthChecks(builder.Configuration);
builder.Services.AddHostedService<HubPaySettingsBootstrapHostedService>();

builder.Services.AddSignalR();
builder.Services.AddHubPaySwagger();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Blazor", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("HubPay:CorsOrigins").Get<string[]>();
        if (configuredOrigins is { Length: > 0 })
        {
            policy.WithOrigins(configuredOrigins);
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            policy.WithOrigins(
                "https://localhost:7239",
                "http://localhost:5176",
                "https://localhost:7061");
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

var bootstrapSettings = app.Configuration.GetSection(HubPaySettings.SectionName).Get<HubPaySettings>()
                      ?? new HubPaySettings();
app.Services.GetRequiredService<IHubPaySettingsProvider>().Initialize(bootstrapSettings);

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Blazor");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.UseHubPaySwagger();

app.MapAuthEndpoints();
app.MapPaymentEndpoints();
app.MapPspAdminEndpoints();
app.MapHub<TransactionHub>(TransactionHub.HubPath);
app.MapHubPayHealthChecks();

app.Run();

public partial class Program;
