using HubPay.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPay.Infrastructure.Payments.MutualTls;

public sealed class PspProductionValidator : IHostedService
{
    private readonly HubPaySettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PspProductionValidator> _logger;

    public PspProductionValidator(
        IOptions<HubPaySettings> options,
        IHostEnvironment environment,
        ILogger<PspProductionValidator> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("Ambiente Development: validação PSP produção ignorada.");
            return Task.CompletedTask;
        }

        ValidatePsp("SIBS", _settings.Sibs);
        ValidatePsp("Bizum", _settings.Bizum);
        ValidatePsp("Wero", _settings.Wero);
        ValidatePsp("CartesBancaires", _settings.CartesBancaires);
        ValidatePsp("iDEAL", _settings.Ideal);
        ValidatePsp("Bancontact", _settings.Bancontact);
        ValidatePsp("Euro6000", _settings.Euro6000);
        ValidatePsp("BancomatPay", _settings.BancomatPay);
        ValidatePsp("Swish", _settings.Swish);
        ValidatePsp("VippsMobilePay", _settings.VippsMobilePay);

        if (_settings.RequireMutualTlsInProduction)
        {
            EnsureMtls("SIBS", _settings.Sibs);
            EnsureMtls("Bizum", _settings.Bizum);
            EnsureMtls("Wero", _settings.Wero);
        }

        _logger.LogInformation("Validação PSP produção concluída com sucesso.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidatePsp(string name, PspEndpointSettings psp)
    {
        if (psp.EnableSimulationFallback)
            throw new InvalidOperationException($"PSP {name}: EnableSimulationFallback deve ser false em produção.");

        if (string.IsNullOrWhiteSpace(psp.BaseUrl))
            throw new InvalidOperationException($"PSP {name}: BaseUrl em falta.");

        if (string.IsNullOrWhiteSpace(psp.ApiKey))
            _logger.LogWarning("PSP {Name}: ApiKey vazia — confirme autenticação apenas por mTLS.", name);
    }

    private void EnsureMtls(string name, PspEndpointSettings psp)
    {
        if (!psp.MutualTls.Enabled)
            throw new InvalidOperationException($"PSP {name}: mTLS obrigatório em produção (MutualTls.Enabled=true).");

        var path = psp.MutualTls.ClientCertificatePath;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"PSP {name}: ClientCertificatePath em falta.");
    }
}
