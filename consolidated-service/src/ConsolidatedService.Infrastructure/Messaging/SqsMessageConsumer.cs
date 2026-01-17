using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsolidatedService.Application.Interfaces;
using ConsolidatedService.Application.UseCases;
using ConsolidatedService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ConsolidatedService.Infrastructure.Resilience;

namespace ConsolidatedService.Infrastructure.Messaging;

public class SqsMessageConsumer : BackgroundService, IMessageConsumer
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SqsMessageConsumer> _logger;
    private readonly SimpleCircuitBreaker _circuitBreaker;

    public SqsMessageConsumer(
        IAmazonSQS sqsClient,
        string queueUrl,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SqsMessageConsumer> logger)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        
        _circuitBreaker = new SimpleCircuitBreaker(
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            logger: logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS message consumer started. Queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = new List<string> { "EventType" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                if (response.Messages != null && response.Messages.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {
                        try
                        {
                            await _circuitBreaker.ExecuteAsync(async () =>
                            {
                                await ProcessMessageAsync(message, stoppingToken);
                            });
                        }
                        catch (InvalidOperationException ex) when (ex.Message == "Circuit breaker is open")
                        {
                            _logger.LogWarning(
                                "Circuit breaker is open. Message not processed. MessageId: {MessageId}. Message will remain in queue. Circuit will remain open for 30 seconds.",
                                message.MessageId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from SQS");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var eventType = message.MessageAttributes.GetValueOrDefault("EventType")?.StringValue;

            if (eventType == "TransactionCreatedEvent")
            {
                _logger.LogInformation("Processing TransactionCreatedEvent. MessageBody: {MessageBody}", message.Body);

                var transactionEvent = JsonSerializer.Deserialize<TransactionCreatedEventDto>(
                    message.Body,
                    new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true
                    });

                if (transactionEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize TransactionCreatedEvent. MessageBody: {MessageBody}", message.Body);
                    return;
                }

                _logger.LogInformation("Deserialized event. TransactionId: {TransactionId}, MerchantId: {MerchantId}, Type: {Type}, Amount: {Amount}, Date: {Date}",
                    transactionEvent.TransactionId, transactionEvent.MerchantId, transactionEvent.Type, transactionEvent.Amount, transactionEvent.TransactionDate);

                using var scope = _serviceScopeFactory.CreateScope();
                var processUseCase = scope.ServiceProvider.GetRequiredService<ProcessTransactionEventUseCase>();

                await processUseCase.ExecuteAsync(
                    transactionEvent.MerchantId,
                    (TransactionType)transactionEvent.Type,
                    transactionEvent.Amount,
                    transactionEvent.TransactionDate,
                    cancellationToken);

                await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = message.ReceiptHandle
                }, cancellationToken);

                _logger.LogInformation(
                    "Transaction event processed successfully. TransactionId: {TransactionId}, MerchantId: {MerchantId}, Type: {Type}, Amount: {Amount}, Date: {Date}",
                    transactionEvent.TransactionId,
                    transactionEvent.MerchantId,
                    transactionEvent.Type,
                    transactionEvent.Amount,
                    DateOnly.FromDateTime(transactionEvent.TransactionDate));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing message. MessageId: {MessageId}",
                message.MessageId);
        }
    }

    private class TransactionCreatedEventDto
    {
        public Guid TransactionId { get; set; }
        public Guid MerchantId { get; set; }
        public int Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
