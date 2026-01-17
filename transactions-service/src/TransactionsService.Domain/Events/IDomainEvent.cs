namespace TransactionsService.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
