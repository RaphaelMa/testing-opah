using Microsoft.EntityFrameworkCore;
using TransactionsService.Domain.Entities;
using TransactionsService.Domain.ValueObjects;

namespace TransactionsService.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Amount).HasConversion(
                v => v.Value,
                v => Amount.Create(v));
            entity.Property(e => e.TransactionDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.TransactionDate);
        });

        base.OnModelCreating(modelBuilder);
    }
}
