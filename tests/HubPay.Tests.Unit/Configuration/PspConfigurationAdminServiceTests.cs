using HubPay.Application.DTOs.Admin;
using HubPay.Domain.Entities;
using HubPay.Infrastructure.Configuration;
using HubPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HubPay.Tests.Unit.Configuration;

public sealed class PspConfigurationAdminServiceTests
{
    [Fact]
    public async Task ReloadAsync_UpdatesSettingsProvider()
    {
        var options = new DbContextOptionsBuilder<HubPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new HubPayDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.PspProviderConfigurations.Add(new PspProviderConfiguration
        {
            Id = Guid.NewGuid(),
            Scheme = "SIBS",
            IsEnabled = true,
            SettingsJson = """{"baseUrl":"https://db.sibs.pt","apiKey":"k1","enableSimulationFallback":false}""",
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var provider = new HubPaySettingsProvider();
        var config = new ConfigurationBuilder().Build();
        var loader = new DatabaseHubPaySettingsLoader(db, config, NullLogger<DatabaseHubPaySettingsLoader>.Instance);
        var service = new PspConfigurationAdminService(db, loader, provider, NullLogger<PspConfigurationAdminService>.Instance);

        var result = await service.ReloadAsync();

        Assert.True(result.Success);
        Assert.Equal("https://db.sibs.pt", provider.Current.Sibs.BaseUrl);
        Assert.Equal("k1", provider.Current.Sibs.ApiKey);
    }

    [Fact]
    public async Task CreateProviderAsync_PersistsRow()
    {
        var options = new DbContextOptionsBuilder<HubPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new HubPayDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var provider = new HubPaySettingsProvider();
        var loader = new DatabaseHubPaySettingsLoader(db, new ConfigurationBuilder().Build(), NullLogger<DatabaseHubPaySettingsLoader>.Instance);
        var service = new PspConfigurationAdminService(db, loader, provider, NullLogger<PspConfigurationAdminService>.Instance);

        var created = await service.CreateProviderAsync(new CreatePspProviderRequest(
            "IDEAL", true, """{"baseUrl":"https://ideal.test"}"""));

        Assert.Equal("IDEAL", created.Scheme);
        Assert.Single(await db.PspProviderConfigurations.ToListAsync());
    }
}
