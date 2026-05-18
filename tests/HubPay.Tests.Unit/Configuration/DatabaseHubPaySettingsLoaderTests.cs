using System.Text.Json;
using HubPay.Domain.Configuration;
using HubPay.Domain.Entities;
using HubPay.Infrastructure.Configuration;
using HubPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HubPay.Tests.Unit.Configuration;

public sealed class DatabaseHubPaySettingsLoaderTests
{
    [Fact]
    public async Task LoadAsync_OverridesSibsFromDatabase()
    {
        var options = new DbContextOptionsBuilder<HubPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new HubPayDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var sibsJson = JsonSerializer.Serialize(new SibsApiSettings
        {
            BaseUrl = "https://custom.sibs.pt",
            ApiKey = "db-api-key",
            DefaultMultibancoEntity = "99999"
        });

        db.PspProviderConfigurations.Add(new PspProviderConfiguration
        {
            Id = Guid.NewGuid(),
            Scheme = "SIBS",
            IsEnabled = true,
            SettingsJson = sibsJson,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HubPay:Sibs:BaseUrl"] = "https://fallback.sibs.pt"
            })
            .Build();

        var loader = new DatabaseHubPaySettingsLoader(db, config, NullLogger<DatabaseHubPaySettingsLoader>.Instance);
        var settings = await loader.LoadAsync();

        Assert.Equal("https://custom.sibs.pt", settings.Sibs.BaseUrl);
        Assert.Equal("db-api-key", settings.Sibs.ApiKey);
        Assert.Equal("99999", settings.Sibs.DefaultMultibancoEntity);
    }
}
