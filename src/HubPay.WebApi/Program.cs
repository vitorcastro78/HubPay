using FluentValidation;
using Microsoft.EntityFrameworkCore;
using HubPay.Application;
using HubPay.Domain.Configuration;
using HubPay.Infrastructure;
using HubPay.Infrastructure.Payments.MutualTls;
using HubPay.Infrastructure.Persistence;
using HubPay.WebApi.Auth;
using HubPay.WebApi.Endpoints;
using HubPay.WebApi.Extensions;
using HubPay.WebApi.Hubs;
using HubPay.WebApi.Middleware;
using HubPay.WebApi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Blazor", policy =>
        policy.WithOrigins(
                "https://localhost:7239",
                "http://localhost:5176",
                "https://localhost:7089",
                "http://localhost:5176")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

var hubPaySettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<HubPaySettings>>().Value;
PspMutualTlsDiagnostics.LogConfiguration(app.Logger, hubPaySettings);
if (hubPaySettings.ApplyMigrationsOnStartup)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HubPayDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Blazor");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapAuthEndpoints();
app.MapPaymentEndpoints();
app.MapHub<TransactionHub>(TransactionHub.HubPath);
app.MapHubPayHealthChecks();

app.Run();

public partial class Program;
