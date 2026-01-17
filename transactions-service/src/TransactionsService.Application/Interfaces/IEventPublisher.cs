using TransactionsService.Domain.Events;

namespace TransactionsService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
