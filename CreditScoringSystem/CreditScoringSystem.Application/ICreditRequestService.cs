using CreditScoringSystem.Application.Commands;
using CreditScoringSystem.Application.Responses;

namespace CreditScoringSystem.Application;
public interface ICreditRequestService
{
    Task<CreditRequestDecisionResponse?> MakeCreditRequestDecision(ScoreCreditRequest command, CancellationToken ct);
}
