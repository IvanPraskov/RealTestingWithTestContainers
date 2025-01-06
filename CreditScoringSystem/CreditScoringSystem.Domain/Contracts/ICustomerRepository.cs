namespace CreditScoringSystem.Domain.Contracts;

public interface ICustomerRepository
{
    Task<Customer?> GetCustomerById(string customerId);
}
