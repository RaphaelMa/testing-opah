using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionsService.Application.Interfaces;
using TransactionsService.Domain.Events;
using TransactionsService.Domain.ValueObjects;
using TransactionsService.Infrastructure.Resilience;

namespace TransactionsService.Infrastructure.Messaging;

public class SqsEventPublisher : IEventPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<SqsEventPublisher> _logger;
    private readonly SimpleCircuitBreaker _circuitBreaker;

    public SqsEventPublisher(
        IAmazonSQS sqsClient,
        string queueUrl,
        ILogger<SqsEventPublisher> logger)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
        _logger = logger;
        
        _circuitBreaker = new SimpleCircuitBreaker(
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            logger: logger);
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await PublishEventInternalAsync(domainEvent, cancellationToken);
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Circuit breaker is open")
        {
            _logger.LogWarning(
                "Circuit breaker is open. Event not published. EventType: {EventType}. Circuit will remain open for 30 seconds.",
                domainEvent.GetType().Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event. EventType: {EventType}",
                domainEvent.GetType().Name);
            throw;
        }
    }

    private async Task PublishEventInternalAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            string messageBody;
            
            if (domainEvent is TransactionCreatedEvent transactionEvent)
            {
                var eventDto = new
                {
                    transactionId = transactionEvent.TransactionId,
                    merchantId = transactionEvent.MerchantId,
                    type = (int)transactionEvent.Type,
                    amount = transactionEvent.Amount,
                    transactionDate = transactionEvent.TransactionDate,
                    occurredAt = transactionEvent.OccurredAt
                };
                
                messageBody = JsonSerializer.Serialize(eventDto, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            else
            {
                messageBody = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            var request = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "EventType",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = domainEvent.GetType().Name
                        }
                    }
                }
            };

            var response = await _sqsClient.SendMessageAsync(request, cancellationToken);

            _logger.LogInformation(
                "Event published successfully. EventType: {EventType}, MessageId: {MessageId}",
                domainEvent.GetType().Name,
                response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event internally. EventType: {EventType}",
                domainEvent.GetType().Name);
            throw;
        }
    }
}
