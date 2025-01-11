namespace CreditScoringSystem.API.CreditRequests.Data.Dtos;

public record CustomerCreditRequest(string CustomerId, decimal RequestedCreditAmount);