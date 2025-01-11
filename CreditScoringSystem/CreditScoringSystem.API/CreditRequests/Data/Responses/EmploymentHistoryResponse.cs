using CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

namespace CreditScoringSystem.API.CreditRequests.Data.Responses;

public record EmploymentHistoryResponse(EmploymentType EmploymentType, int EmploymentDurationInMonths, decimal CurrentNetMonthlyIncome);
