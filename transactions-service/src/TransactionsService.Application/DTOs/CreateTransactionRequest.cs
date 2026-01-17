using TransactionsService.Domain.ValueObjects;

namespace TransactionsService.Application.DTOs;

public class CreateTransactionRequest
{
    public Guid MerchantId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
}
