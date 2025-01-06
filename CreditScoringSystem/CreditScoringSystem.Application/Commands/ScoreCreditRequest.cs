namespace CreditScoringSystem.Application.Commands;

public record ScoreCreditRequest(string CustomerId, decimal RequestedAmount);
