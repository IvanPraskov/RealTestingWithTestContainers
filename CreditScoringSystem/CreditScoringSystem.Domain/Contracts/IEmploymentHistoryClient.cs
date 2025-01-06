namespace CreditScoringSystem.Domain.Contracts;

public interface IEmploymentHistoryClient
{
    Task<EmploymentHistoryResponse?> GetCustomerEmploymentHistory(string customerId, CancellationToken ct);
}
