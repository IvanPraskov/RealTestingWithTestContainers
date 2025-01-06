namespace CreditScoringSystem.Application.Responses;

public record CreditRequestDecisionResponse(string CustomerId, string CreditRequestDecision, decimal MaxCreditAmount);
