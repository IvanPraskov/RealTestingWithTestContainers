namespace CreditScoringSystem.API.CreditRequests.Data.Dtos;

public sealed class CustomerDto
{
    public required string CustomerId { get; init; }

    public required int Age { get; init; }
}
