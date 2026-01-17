using FluentAssertions;
using Xunit;
using TransactionsService.Domain.ValueObjects;

namespace TransactionsService.Tests.Unit.Domain;

public class AmountTests
{
    [Fact]
    public void Create_WithValidAmount_ShouldCreateAmount()
    {
        var amount = Amount.Create(100.50m);

        amount.Should().NotBeNull();
        amount.Value.Should().Be(100.50m);
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrowArgumentException()
    {
        var action = () => Amount.Create(0m);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Amount must be greater than zero.*");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowArgumentException()
    {
        var action = () => Amount.Create(-10m);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Amount must be greater than zero.*");
    }

    [Fact]
    public void ImplicitConversion_ShouldConvertToDecimal()
    {
        var amount = Amount.Create(100.50m);
        decimal value = amount;

        value.Should().Be(100.50m);
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        var amount1 = Amount.Create(100m);
        var amount2 = Amount.Create(100m);

        amount1.Equals(amount2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        var amount1 = Amount.Create(100m);
        var amount2 = Amount.Create(200m);

        amount1.Equals(amount2).Should().BeFalse();
    }
}
