using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using TransactionsService.Application.Interfaces;
using TransactionsService.Application.UseCases;
using TransactionsService.Domain.Repositories;
using TransactionsService.Infrastructure.Data;
using TransactionsService.Infrastructure.Messaging;
using TransactionsService.Infrastructure.Repositories;
using TransactionsService.Api.Middleware;
using TransactionsService.Api.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var queueUrl = builder.Configuration["AWS:QueueUrl"] ?? throw new InvalidOperationException("AWS:QueueUrl configuration is required");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddCheck<SqsHealthCheck>("sqs", tags: new[] { "infrastructure" });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<CreateTransactionUseCase>();

var sqsEndpoint = builder.Configuration["AWS:SQSEndpoint"];
var awsOptions = new AWSOptions
{
    DefaultClientConfig = { ServiceURL = sqsEndpoint },
    Credentials = new BasicAWSCredentials("test", "test"),
    Region = Amazon.RegionEndpoint.USEast1
};
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonSQS>();

builder.Services.AddSingleton<SqsHealthCheck>(sp =>
{
    var sqsClient = sp.GetRequiredService<IAmazonSQS>();
    return new SqsHealthCheck(sqsClient, queueUrl);
});

builder.Services.AddScoped<IEventPublisher>(sp =>
{
    var sqsClient = sp.GetRequiredService<IAmazonSQS>();
    var logger = sp.GetRequiredService<ILogger<SqsEventPublisher>>();
    return new SqsEventPublisher(sqsClient, queueUrl, logger);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
