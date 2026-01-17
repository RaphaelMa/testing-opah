using TransactionsService.Domain.ValueObjects;

namespace TransactionsService.Domain.Events;

public class TransactionCreatedEvent : IDomainEvent
{
    public Guid TransactionId { get; }
    public Guid MerchantId { get; }
    public TransactionType Type { get; }
    public decimal Amount { get; }
    public DateTime TransactionDate { get; }
    public DateTime OccurredAt { get; }

    public TransactionCreatedEvent(
        Guid transactionId,
        Guid merchantId,
        TransactionType type,
        decimal amount,
        DateTime transactionDate)
    {
        TransactionId = transactionId;
        MerchantId = merchantId;
        Type = type;
        Amount = amount;
        TransactionDate = transactionDate;
        OccurredAt = DateTime.UtcNow;
    }
}
