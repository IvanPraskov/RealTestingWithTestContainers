using CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

namespace CreditScoringSystem.API.CreditRequests.Data.Database;

internal class CreditRequestDbModel
{
    public required string CustomerId { get; init; }

    public required decimal RequestedAmount { get; init; }

    public required CreditRequestDecision CreditRequestDecisionId { get; init; }

    public decimal? MaxCreditAmount { get; init; }

    public required int CustomerScore { get; init; }
}
