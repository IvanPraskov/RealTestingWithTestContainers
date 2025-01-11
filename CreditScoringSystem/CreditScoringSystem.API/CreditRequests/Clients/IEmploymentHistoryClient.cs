using CreditScoringSystem.API.CreditRequests.Data.Responses;

namespace CreditScoringSystem.API.CreditRequests.Clients;

public interface IEmploymentHistoryClient
{
    Task<EmploymentHistoryResponse?> GetCustomerEmploymentHistory(string customerId, CancellationToken ct);
}
