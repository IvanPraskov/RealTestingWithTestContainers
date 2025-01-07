using CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

namespace CreditScoringSystem.API.CreditRequests;

internal static class CreditRequestsEndpoints
{
    internal static void MapCreditRequests(this IEndpointRouteBuilder app)
        => app.MapMakeCreditDecision();
}
