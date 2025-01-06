namespace CreditScoringSystem.Domain.Contracts;

public record EmploymentHistoryResponse(EmploymentType EmploymentType, int EmploymentDurationInMonths, decimal CurrentNetMonthlyIncome);
