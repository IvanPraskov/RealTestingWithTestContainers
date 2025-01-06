namespace CreditScoringSystem.Domain;

public sealed class Customer
{
    public required string CustomerId { get; init; }

    public required int Age { get; init; }
}
