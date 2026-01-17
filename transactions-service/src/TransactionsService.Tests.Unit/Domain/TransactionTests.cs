using FluentAssertions;
using Xunit;
using TransactionsService.Domain.Entities;
using TransactionsService.Domain.Events;
using TransactionsService.Domain.ValueObjects;

namespace TransactionsService.Tests.Unit.Domain;

public class TransactionTests
{
    [Fact]
    public void Create_ShouldCreateTransactionWithValidData()
    {
        var merchantId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;

        var transaction = Transaction.Create(
            merchantId,
            TransactionType.Credit,
            100.50m,
            transactionDate,
            "Test description");

        transaction.Should().NotBeNull();
        transaction.Id.Should().NotBeEmpty();
        transaction.MerchantId.Should().Be(merchantId);
        transaction.Type.Should().Be(TransactionType.Credit);
        transaction.Amount.Value.Should().Be(100.50m);
        transaction.TransactionDate.Should().Be(transactionDate);
        transaction.Description.Should().Be("Test description");
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldRaiseTransactionCreatedEvent()
    {
        var merchantId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;

        var transaction = Transaction.Create(
            merchantId,
            TransactionType.Debit,
            50.00m,
            transactionDate);

        transaction.DomainEvents.Should().HaveCount(1);
        transaction.DomainEvents.First().Should().BeOfType<TransactionCreatedEvent>();

        var @event = transaction.DomainEvents.First() as TransactionCreatedEvent;
        @event.Should().NotBeNull();
        @event!.TransactionId.Should().Be(transaction.Id);
        @event.MerchantId.Should().Be(merchantId);
        @event.Type.Should().Be(TransactionType.Debit);
        @event.Amount.Should().Be(50.00m);
        @event.TransactionDate.Should().Be(transactionDate);
    }

    [Fact]
    public void ClearEvents_ShouldRemoveAllDomainEvents()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            TransactionType.Credit,
            100m,
            DateTime.UtcNow);

        transaction.DomainEvents.Should().HaveCount(1);

        transaction.ClearEvents();

        transaction.DomainEvents.Should().BeEmpty();
    }
}
