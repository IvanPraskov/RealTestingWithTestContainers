namespace CreditScoringSystem.Infrastructure.DbModels;

internal record CustomerDbModel
{
    public required string CustomerId { get; init; }

    public required DateTime DateOfBirth { get; init; }

    public required string CustomerFirstName { get; init; }

    public string? CustomerMiddleName { get; init; }

    public required string CustomerLastName { get; init; }
}
