namespace CreditScoringSystem.Domain.Dtos;

public record CreditHistoryDto(string CustomerId, int MissedPayments, decimal? ExistingMonthlyDebt);
