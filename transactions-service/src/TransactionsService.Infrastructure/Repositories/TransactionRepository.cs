using Microsoft.EntityFrameworkCore;
using TransactionsService.Domain.Entities;
using TransactionsService.Domain.Repositories;
using TransactionsService.Infrastructure.Data;

namespace TransactionsService.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
