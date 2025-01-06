namespace CreditScoringSystem.Domain.Dtos;

public record EmploymentHistoryDto(EmploymentType EmploymentType, int EmploymentDurationInMonths, decimal CurrentNetMonthlyIncome);
