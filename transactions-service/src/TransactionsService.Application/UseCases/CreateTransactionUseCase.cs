using TransactionsService.Application.DTOs;
using TransactionsService.Application.Interfaces;
using TransactionsService.Domain.Entities;
using TransactionsService.Domain.Events;
using TransactionsService.Domain.Repositories;

namespace TransactionsService.Application.UseCases;

public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IEventPublisher _eventPublisher;

    public CreateTransactionUseCase(
        ITransactionRepository transactionRepository,
        IEventPublisher eventPublisher)
    {
        _transactionRepository = transactionRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<TransactionResponse> ExecuteAsync(
        CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = Transaction.Create(
            request.MerchantId,
            request.Type,
            request.Amount,
            request.TransactionDate,
            request.Description);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        var events = transaction.DomainEvents.ToList();
        transaction.ClearEvents();

        foreach (var domainEvent in events)
        {
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        return new TransactionResponse
        {
            Id = transaction.Id,
            MerchantId = transaction.MerchantId,
            Type = transaction.Type,
            Amount = transaction.Amount.Value,
            TransactionDate = transaction.TransactionDate,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }
}
