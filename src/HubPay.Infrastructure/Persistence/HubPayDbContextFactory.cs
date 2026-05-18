using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HubPay.Infrastructure.Persistence;

public sealed class HubPayDbContextFactory : IDesignTimeDbContextFactory<HubPayDbContext>
{
    public HubPayDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HubPayDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("HUBPAY_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=hubpay_dev;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);
        return new HubPayDbContext(optionsBuilder.Options);
    }
}
