namespace SajhaSikshya.Data.ValueObjects;

/// <summary>
/// Immutable monetary amount paired with a currency code. The only invariant this
/// type enforces is "money can't be negative" — domain-specific limits (e.g. a
/// listing's maximum price) belong in the caller, not baked into this general-purpose
/// value object. Designed to be usable as an EF Core complex/owned type once an entity
/// (e.g. the future Listing.Price) needs one.
/// </summary>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency = "NPR")
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Money cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "NPR") => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>Throws if the result would be negative — Money has no representation for a debt/deficit.</summary>
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static bool operator ==(Money left, Money right) => left.Equals(right);

    public static bool operator !=(Money left, Money right) => !left.Equals(right);

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public bool Equals(Money other) => Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    /// <summary>Formats as e.g. "Rs. 1,500.00"; falls back to the ISO code for currencies without a known symbol.</summary>
    public override string ToString()
    {
        var symbol = Currency switch
        {
            "NPR" => "Rs. ",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => Currency + " ",
        };

        return $"{symbol}{Amount:N2}";
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException($"Cannot combine or compare {left.Currency} with {right.Currency}.");
        }
    }
}
