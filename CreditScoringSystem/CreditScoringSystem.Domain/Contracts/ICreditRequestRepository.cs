namespace CreditScoringSystem.Domain.Contracts;

public interface ICreditRequestRepository
{
    Task SaveCreditRequest(CreditRequest creditRequest);
}
