using FluentAssertions;
using Moq;
using Xunit;
using TransactionsService.Application.DTOs;
using TransactionsService.Application.Interfaces;
using TransactionsService.Application.UseCases;
using TransactionsService.Domain.Entities;
using TransactionsService.Domain.Events;
using TransactionsService.Domain.Repositories;
using TransactionsService.Domain.ValueObjects;

namespace TransactionsService.Tests.Unit.UseCases;

public class CreateTransactionUseCaseTests
{
    private readonly Mock<ITransactionRepository> _repositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateTransactionUseCase _useCase;

    public CreateTransactionUseCaseTests()
    {
        _repositoryMock = new Mock<ITransactionRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _useCase = new CreateTransactionUseCase(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateTransactionAndPublishEvent()
    {
        var request = new CreateTransactionRequest
        {
            MerchantId = Guid.NewGuid(),
            Type = TransactionType.Credit,
            Amount = 100.50m,
            TransactionDate = DateTime.UtcNow,
            Description = "Test transaction"
        };

        Transaction? savedTransaction = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                savedTransaction = t;
                return t;
            });

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(e => e.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _useCase.ExecuteAsync(request);

        result.Should().NotBeNull();
        result.MerchantId.Should().Be(request.MerchantId);
        result.Type.Should().Be(request.Type);
        result.Amount.Should().Be(request.Amount);
        result.Description.Should().Be(request.Description);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(e => e.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPublishEventOnlyAfterSuccessfulPersistence()
    {
        var request = new CreateTransactionRequest
        {
            MerchantId = Guid.NewGuid(),
            Type = TransactionType.Debit,
            Amount = 50.00m,
            TransactionDate = DateTime.UtcNow
        };

        var saveChangesCalled = false;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) => t);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => saveChangesCalled = true)
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(e => e.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => saveChangesCalled.Should().BeTrue())
            .Returns(Task.CompletedTask);

        await _useCase.ExecuteAsync(request);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(e => e.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
