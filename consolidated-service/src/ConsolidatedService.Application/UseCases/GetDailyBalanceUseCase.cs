using ConsolidatedService.Application.DTOs;
using ConsolidatedService.Domain.Repositories;

namespace ConsolidatedService.Application.UseCases;

public class GetDailyBalanceUseCase
{
    private readonly IDailyBalanceRepository _repository;

    public GetDailyBalanceUseCase(IDailyBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyBalanceResponse?> ExecuteAsync(
        Guid merchantId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var dailyBalance = await _repository.GetByMerchantAndDateAsync(merchantId, date, cancellationToken);

        if (dailyBalance == null)
        {
            return null;
        }

        return new DailyBalanceResponse
        {
            Id = dailyBalance.Id,
            MerchantId = dailyBalance.MerchantId,
            BalanceDate = dailyBalance.BalanceDate,
            TotalCredits = dailyBalance.TotalCredits,
            TotalDebits = dailyBalance.TotalDebits,
            NetBalance = dailyBalance.NetBalance,
            LastUpdatedAt = dailyBalance.LastUpdatedAt
        };
    }
}
