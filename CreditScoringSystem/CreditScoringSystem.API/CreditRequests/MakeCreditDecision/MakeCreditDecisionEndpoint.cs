using CreditScoringSystem.API.CreditRequests.Clients;
using CreditScoringSystem.API.CreditRequests.Data.Database;
using CreditScoringSystem.API.CreditRequests.Data.Dtos;

namespace CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

internal static class MakeCreditDecisionEndpoint
{
    internal static void MapMakeCreditDecision(this IEndpointRouteBuilder app) => app.MapPost(CreditRequestsApiPaths.MakeCreditDecision,
        async (CustomerCreditRequest request,
            ICreditRequestPersistance persistence,
            IEmploymentHistoryClient employmentHistoryClient,
            CancellationToken ct) =>
        {
            if (request.CustomerId.Length != 10 || request.RequestedCreditAmount <= 0)
            {
                return Results.BadRequest();
            }

            var customer = await persistence.GetCustomerById(request.CustomerId);
            if (customer is null)
            {
                return Results.BadRequest();
            }

            var emplHistoryResponse = await employmentHistoryClient.GetCustomerEmploymentHistory(request.CustomerId, ct);
            if (emplHistoryResponse is null)
            {
                return Results.BadRequest();
            }

            EmploymentHistoryDto employmentHistory = new(
                                                         emplHistoryResponse.EmploymentDurationInMonths,
                                                         emplHistoryResponse.CurrentNetMonthlyIncome);
            var creditHistory = await persistence.GetCustomerCreditHistory(request.CustomerId);
            var customerScore = CreditScoreCalculator.Calculate(creditHistory, employmentHistory);
            var (decision, maxCreditAmount) = CreditDecisionMaker.Decide(customerScore, request.RequestedCreditAmount, employmentHistory.CurrentNetMonthlyIncome);
            CreditRequestDbModel creditRequestModel = new()
            {
                CreditRequestDecisionId = decision,
                CustomerScore = customerScore,
                CustomerId = request.CustomerId,
                MaxCreditAmount = maxCreditAmount,
                RequestedAmount = request.RequestedCreditAmount,
            };
            await persistence.SaveCreditRequest(creditRequestModel);

            CreditRequestDecisionResponse response = new(request.CustomerId, MapScoringDecision(decision), maxCreditAmount);
            return Results.Ok(response);
        });

    private static string MapScoringDecision(CreditRequestDecision scoringResultDecision) => scoringResultDecision switch
    {
        CreditRequestDecision.Approved => "Approved",
        CreditRequestDecision.ManualReviewRequired => "For manual review",
        CreditRequestDecision.Rejected => "Rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(scoringResultDecision)),
    };
}
