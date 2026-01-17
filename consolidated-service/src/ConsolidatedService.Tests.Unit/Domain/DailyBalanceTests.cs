using FluentAssertions;
using Xunit;
using ConsolidatedService.Domain.Entities;

namespace ConsolidatedService.Tests.Unit.Domain;

public class DailyBalanceTests
{
    [Fact]
    public void Create_ShouldCreateDailyBalanceWithValidData()
    {
        var merchantId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var dailyBalance = DailyBalance.Create(merchantId, date);

        dailyBalance.Should().NotBeNull();
        dailyBalance.Id.Should().NotBeEmpty();
        dailyBalance.MerchantId.Should().Be(merchantId);
        dailyBalance.BalanceDate.Should().Be(date);
        dailyBalance.TotalCredits.Should().Be(0);
        dailyBalance.TotalDebits.Should().Be(0);
        dailyBalance.NetBalance.Should().Be(0);
    }

    [Fact]
    public void AddCredit_ShouldIncreaseTotalCreditsAndNetBalance()
    {
        var dailyBalance = DailyBalance.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));
        var initialCredits = dailyBalance.TotalCredits;
        var initialBalance = dailyBalance.NetBalance;

        dailyBalance.AddCredit(100.50m);

        dailyBalance.TotalCredits.Should().Be(initialCredits + 100.50m);
        dailyBalance.NetBalance.Should().Be(initialBalance + 100.50m);
    }

    [Fact]
    public void AddDebit_ShouldIncreaseTotalDebitsAndDecreaseNetBalance()
    {
        var dailyBalance = DailyBalance.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));
        var initialDebits = dailyBalance.TotalDebits;
        var initialBalance = dailyBalance.NetBalance;

        dailyBalance.AddDebit(50.25m);

        dailyBalance.TotalDebits.Should().Be(initialDebits + 50.25m);
        dailyBalance.NetBalance.Should().Be(initialBalance - 50.25m);
    }

    [Fact]
    public void NetBalance_ShouldBeCreditsMinusDebits()
    {
        var dailyBalance = DailyBalance.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));

        dailyBalance.AddCredit(200m);
        dailyBalance.AddDebit(50m);

        dailyBalance.NetBalance.Should().Be(150m);
    }
}
