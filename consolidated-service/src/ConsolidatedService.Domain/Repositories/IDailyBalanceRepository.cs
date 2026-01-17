using ConsolidatedService.Domain.Entities;

namespace ConsolidatedService.Domain.Repositories;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByMerchantAndDateAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<DailyBalance>> GetByMerchantAndDateRangeAsync(Guid merchantId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<DailyBalance> AddAsync(DailyBalance dailyBalance, CancellationToken cancellationToken = default);
    Task UpdateAsync(DailyBalance dailyBalance, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
