namespace CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

public record CreditRequestDecisionResponse(string CustomerId, string CreditRequestDecision, decimal MaxCreditAmount);
