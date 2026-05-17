using HubPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HubPay.Infrastructure.Persistence;

public sealed class HubPayDbContext : DbContext
{
    public HubPayDbContext(DbContextOptions<HubPayDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.PaymentScheme).HasMaxLength(32).IsRequired();
            entity.Property(e => e.EndToEndId).HasMaxLength(35).IsRequired();
            entity.HasIndex(e => e.EndToEndId).IsUnique();
            entity.Property(e => e.CustomerIP).HasMaxLength(45);
            entity.Property(e => e.DeviceFingerprint).HasMaxLength(256);
            entity.Property(e => e.CustomerEmail).HasMaxLength(256);
            entity.Property(e => e.ScaStatus).HasMaxLength(32);
            entity.Property(e => e.AntiFraudScore).HasPrecision(5, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.ExternalReference).HasMaxLength(128);
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.Property(e => e.NetSettledAmount).HasPrecision(18, 2);
            entity.Property(e => e.ProcessingFee).HasPrecision(18, 2);
        });
    }
}
