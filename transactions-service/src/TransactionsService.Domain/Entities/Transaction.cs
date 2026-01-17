using TransactionsService.Domain.ValueObjects;
using TransactionsService.Domain.Events;

namespace TransactionsService.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public TransactionType Type { get; private set; }
    public Amount Amount { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction() { }

    private Transaction(
        Guid id,
        Guid merchantId,
        TransactionType type,
        Amount amount,
        DateTime transactionDate,
        string? description)
    {
        Id = id;
        MerchantId = merchantId;
        Type = type;
        Amount = amount;
        TransactionDate = transactionDate;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public static Transaction Create(
        Guid merchantId,
        TransactionType type,
        decimal amount,
        DateTime transactionDate,
        string? description = null)
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            merchantId,
            type,
            Amount.Create(amount),
            transactionDate,
            description);

        transaction.AddEvent(new TransactionCreatedEvent(
            transaction.Id,
            transaction.MerchantId,
            transaction.Type,
            transaction.Amount.Value,
            transaction.TransactionDate));

        return transaction;
    }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearEvents()
    {
        _domainEvents.Clear();
    }
}
