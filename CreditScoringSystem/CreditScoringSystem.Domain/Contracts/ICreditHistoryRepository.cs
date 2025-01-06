using CreditScoringSystem.Domain.Dtos;

namespace CreditScoringSystem.Domain.Contracts;

public interface ICreditHistoryRepository
{
    Task<CreditHistoryDto?> GetCustomerCreditHistory(string customerId);
}
