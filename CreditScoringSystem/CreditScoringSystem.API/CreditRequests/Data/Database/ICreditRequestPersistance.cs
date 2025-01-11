using CreditScoringSystem.API.CreditRequests.Data.Dtos;

namespace CreditScoringSystem.API.CreditRequests.Data.Database;

internal interface ICreditRequestPersistance
{
    Task<CustomerDto?> GetCustomerById(string customerId);
    Task SaveCreditRequest(CreditRequestDbModel creditRequest);
    Task<CreditHistoryDto?> GetCustomerCreditHistory(string customerId);
}