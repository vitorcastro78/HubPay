using HubPay.Application.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace HubPay.Frontend.Blazor.Services;

public sealed class TransactionHubClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _defaultApiBaseUrl;

    public TransactionHubClient(IConfiguration configuration) =>
        _defaultApiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7239/";

    public event Action<TransactionDto>? OnTransactionUpdated;

    public async Task ConnectAsync(string? apiBaseUrl = null, string? accessToken = null)
    {
        var baseUrl = apiBaseUrl ?? _defaultApiBaseUrl;
        var hubUrl = new Uri(new Uri(baseUrl), "/hubs/transactions");
        var builder = new HubConnectionBuilder().WithUrl(hubUrl, options =>
        {
            if (!string.IsNullOrEmpty(accessToken))
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
        });

        _connection = builder.WithAutomaticReconnect().Build();
        _connection.On<TransactionDto>("TransactionUpdated", dto => OnTransactionUpdated?.Invoke(dto));
        await _connection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
