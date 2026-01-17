namespace ConsolidatedService.Application.DTOs;

public class DailyBalanceResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public DateOnly BalanceDate { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal NetBalance { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
