using CreditScoringSystem.Application.Commands;

namespace CreditScoringSystem.Application;
public interface ICreditRequestScoringService
{
    Task ScoreCreditRequest(ScoreCreditRequest command);
}
