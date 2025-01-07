using CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

namespace CreditScoringSystem.API.CreditRequests.Data.Dtos;

public record EmploymentHistoryDto(EmploymentType EmploymentType, int EmploymentDurationInMonths, decimal CurrentNetMonthlyIncome);
