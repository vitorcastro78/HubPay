using System.Net.Http.Json;
using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace HubPay.Infrastructure.Payments;

public sealed class PspApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly PspEndpointSettings _settings;
    private readonly string _scheme;
    private readonly ILogger _logger;

    public PspApiClient(HttpClient httpClient, PspEndpointSettings settings, string scheme, ILogger logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _scheme = scheme;
        _logger = logger;
    }

    public async Task<JsonElement> PostAsync<TRequest>(string path, TRequest body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = JsonContent.Create(body);
        ApplyHeaders(request);
        return await SendAsync(request, ct);
    }

    public async Task<JsonElement> GetAsync(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        ApplyHeaders(request);
        return await SendAsync(request, ct);
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("X-Request-Id", Guid.NewGuid().ToString("N"));
        request.Headers.TryAddWithoutValidation("X-Client-Id", _settings.ClientId);

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            request.Headers.TryAddWithoutValidation("X-API-Key", _settings.ApiKey);

        if (!string.IsNullOrWhiteSpace(_settings.MerchantId))
            request.Headers.TryAddWithoutValidation("X-Merchant-Id", _settings.MerchantId);
    }

    private async Task<JsonElement> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "PSP {Scheme} erro HTTP {Status} em {Method} {Path}: {Body}",
                    _scheme, (int)response.StatusCode, request.Method, request.RequestUri, body);

                throw new PspIntegrationException(
                    _scheme,
                    $"PSP {_scheme} respondeu {(int)response.StatusCode}",
                    (int)response.StatusCode,
                    body);
            }

            if (string.IsNullOrWhiteSpace(body))
                return JsonSerializer.SerializeToElement(new { });

            return JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
        }
        catch (PspIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PspIntegrationException(_scheme, $"Falha de comunicação com PSP {_scheme}", ex);
        }
    }
}
