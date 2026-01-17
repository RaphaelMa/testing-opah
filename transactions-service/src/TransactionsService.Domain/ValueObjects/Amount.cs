namespace TransactionsService.Domain.ValueObjects;

public class Amount
{
    public decimal Value { get; private set; }

    private Amount(decimal value)
    {
        Value = value;
    }

    public static Amount Create(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(value));
        }

        return new Amount(value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Amount other)
            return false;

        return Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static implicit operator decimal(Amount amount) => amount.Value;
}
