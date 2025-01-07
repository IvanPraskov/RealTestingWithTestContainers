namespace CreditScoringSystem.API.CreditRequests.Data.Database;

internal record CreditHistoryDbModel
{
    public required string CustomerId { get; init; }

    public required int MissedPayments { get; init; }

    public decimal? ExistingMonthlyDebt { get; init; }
}
