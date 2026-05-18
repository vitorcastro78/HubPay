using HubPay.Infrastructure.Clearing;
using HubPay.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HubPay.Tests.Integration;

public sealed class WebApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HubPay:ApplyMigrationsOnStartup"] = "false",
                ["HubPay:LoadPspConfigurationFromDatabase"] = "false",
                ["HubPay:RedisConnectionString"] = "localhost:6379,abortConnect=false",
                ["HubPay:ClearingIntervalSeconds"] = "3600",
                ["HubPay:RequireMutualTlsInProduction"] = "false",
                ["HubPay:Sibs:EnableSimulationFallback"] = "true",
                ["HubPay:Sibs:MutualTls:Enabled"] = "false",
                ["HubPay:Bizum:EnableSimulationFallback"] = "true",
                ["HubPay:Wero:EnableSimulationFallback"] = "true",
                ["HubPay:CartesBancaires:EnableSimulationFallback"] = "true",
                ["HubPay:Ideal:EnableSimulationFallback"] = "true",
                ["HubPay:Bancontact:EnableSimulationFallback"] = "true",
                ["HubPay:Euro6000:EnableSimulationFallback"] = "true",
                ["HubPay:BancomatPay:EnableSimulationFallback"] = "true",
                ["HubPay:Swish:EnableSimulationFallback"] = "true",
                ["HubPay:VippsMobilePay:EnableSimulationFallback"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<HubPayDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<HubPayDbContext>(options =>
                options.UseInMemoryDatabase($"HubPayTests-{Guid.NewGuid()}"));

            var clearingHosts = services
                .Where(d => d.ImplementationType == typeof(FinancialClearingEngine))
                .ToList();
            foreach (var descriptor in clearingHosts)
                services.Remove(descriptor);
        });
    }
}
