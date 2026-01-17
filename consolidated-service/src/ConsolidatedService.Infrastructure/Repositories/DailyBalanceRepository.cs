using Microsoft.EntityFrameworkCore;
using ConsolidatedService.Domain.Entities;
using ConsolidatedService.Domain.Repositories;
using ConsolidatedService.Infrastructure.Data;

namespace ConsolidatedService.Infrastructure.Repositories;

public class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly ApplicationDbContext _context;

    public DailyBalanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DailyBalance?> GetByMerchantAndDateAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.DailyBalances
            .AsTracking()
            .FirstOrDefaultAsync(
                db => db.MerchantId == merchantId && db.BalanceDate == date,
                cancellationToken);
    }

    public async Task<IEnumerable<DailyBalance>> GetByMerchantAndDateRangeAsync(Guid merchantId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.DailyBalances
            .Where(db => db.MerchantId == merchantId && db.BalanceDate >= startDate && db.BalanceDate <= endDate)
            .OrderBy(db => db.BalanceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<DailyBalance> AddAsync(DailyBalance dailyBalance, CancellationToken cancellationToken = default)
    {
        await _context.DailyBalances.AddAsync(dailyBalance, cancellationToken);
        return dailyBalance;
    }

    public Task UpdateAsync(DailyBalance dailyBalance, CancellationToken cancellationToken = default)
    {
        _context.DailyBalances.Update(dailyBalance);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
