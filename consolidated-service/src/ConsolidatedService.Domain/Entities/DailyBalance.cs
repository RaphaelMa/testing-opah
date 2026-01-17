using ConsolidatedService.Domain.ValueObjects;

namespace ConsolidatedService.Domain.Entities;

public class DailyBalance
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public DateOnly BalanceDate { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public decimal NetBalance { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DailyBalance() { }

    private DailyBalance(
        Guid id,
        Guid merchantId,
        DateOnly balanceDate,
        decimal totalCredits,
        decimal totalDebits)
    {
        Id = id;
        MerchantId = merchantId;
        BalanceDate = balanceDate;
        TotalCredits = totalCredits;
        TotalDebits = totalDebits;
        NetBalance = totalCredits - totalDebits;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public static DailyBalance Create(
        Guid merchantId,
        DateOnly balanceDate,
        decimal totalCredits = 0,
        decimal totalDebits = 0)
    {
        return new DailyBalance(
            Guid.NewGuid(),
            merchantId,
            balanceDate,
            totalCredits,
            totalDebits);
    }

    public void AddCredit(decimal amount)
    {
        TotalCredits += amount;
        RecalculateBalance();
    }

    public void AddDebit(decimal amount)
    {
        TotalDebits += amount;
        RecalculateBalance();
    }

    private void RecalculateBalance()
    {
        NetBalance = TotalCredits - TotalDebits;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
