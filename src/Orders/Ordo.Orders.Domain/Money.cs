namespace Ordo.Orders.Domain;

public sealed record Money
{
    public Money(long amountMinor, string currency)
    {
        if (amountMinor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsLetter))
        {
            throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", nameof(currency));
        }

        AmountMinor = amountMinor;
        Currency = currency.ToUpperInvariant();
    }

    public long AmountMinor { get; }

    public string Currency { get; }
}
