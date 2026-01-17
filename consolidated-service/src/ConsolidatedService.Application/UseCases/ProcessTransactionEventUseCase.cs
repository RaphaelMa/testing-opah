using ConsolidatedService.Domain.Entities;
using ConsolidatedService.Domain.Repositories;
using ConsolidatedService.Domain.ValueObjects;

namespace ConsolidatedService.Application.UseCases;

public class ProcessTransactionEventUseCase
{
    private readonly IDailyBalanceRepository _repository;

    public ProcessTransactionEventUseCase(IDailyBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(
        Guid merchantId,
        TransactionType type,
        decimal amount,
        DateTime transactionDate,
        CancellationToken cancellationToken = default)
    {
        var balanceDate = DateOnly.FromDateTime(transactionDate);

        var dailyBalance = await _repository.GetByMerchantAndDateAsync(merchantId, balanceDate, cancellationToken);

        if (dailyBalance == null)
        {
            dailyBalance = DailyBalance.Create(merchantId, balanceDate);
            await _repository.AddAsync(dailyBalance, cancellationToken);
        }

        if (type == TransactionType.Credit)
        {
            dailyBalance.AddCredit(amount);
        }
        else
        {
            dailyBalance.AddDebit(amount);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
