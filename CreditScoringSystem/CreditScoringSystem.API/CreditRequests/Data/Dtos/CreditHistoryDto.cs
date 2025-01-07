namespace CreditScoringSystem.API.CreditRequests.Data.Dtos;

public record CreditHistoryDto(string CustomerId, int MissedPayments, decimal? ExistingMonthlyDebt);
