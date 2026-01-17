using ConsolidatedService.Application.DTOs;
using ConsolidatedService.Domain.Repositories;

namespace ConsolidatedService.Application.UseCases;

public class GetDailyBalanceRangeUseCase
{
    private readonly IDailyBalanceRepository _repository;

    public GetDailyBalanceRangeUseCase(IDailyBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<DailyBalanceResponse>> ExecuteAsync(
        Guid merchantId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var dailyBalances = await _repository.GetByMerchantAndDateRangeAsync(merchantId, startDate, endDate, cancellationToken);

        return dailyBalances.Select(db => new DailyBalanceResponse
        {
            Id = db.Id,
            MerchantId = db.MerchantId,
            BalanceDate = db.BalanceDate,
            TotalCredits = db.TotalCredits,
            TotalDebits = db.TotalDebits,
            NetBalance = db.NetBalance,
            LastUpdatedAt = db.LastUpdatedAt
        });
    }
}
