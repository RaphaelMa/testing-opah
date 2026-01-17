using Microsoft.EntityFrameworkCore;
using ConsolidatedService.Domain.Entities;

namespace ConsolidatedService.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DailyBalance> DailyBalances { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.BalanceDate).IsRequired();
            entity.Property(e => e.TotalCredits).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.TotalDebits).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.NetBalance).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastUpdatedAt).IsRequired();
            entity.HasIndex(e => new { e.MerchantId, e.BalanceDate }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
