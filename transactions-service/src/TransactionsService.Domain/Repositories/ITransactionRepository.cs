using TransactionsService.Domain.Entities;

namespace TransactionsService.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
