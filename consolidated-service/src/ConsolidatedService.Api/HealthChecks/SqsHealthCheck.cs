using Amazon.SQS;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConsolidatedService.Api.HealthChecks;

public class SqsHealthCheck : IHealthCheck
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsHealthCheck(IAmazonSQS sqsClient, string queueUrl)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new Amazon.SQS.Model.GetQueueAttributesRequest
            {
                QueueUrl = _queueUrl,
                AttributeNames = new List<string> { "QueueArn" }
            };
            await _sqsClient.GetQueueAttributesAsync(request, cancellationToken);
            return HealthCheckResult.Healthy("SQS connection available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQS connection unavailable", ex);
        }
    }
}
