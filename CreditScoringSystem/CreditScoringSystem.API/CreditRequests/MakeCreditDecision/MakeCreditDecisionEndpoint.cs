using CreditScoringSystem.API.CreditRequests.Clients;
using CreditScoringSystem.API.CreditRequests.Data.Database;
using CreditScoringSystem.API.CreditRequests.Data.Dtos;

namespace CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

internal static class MakeCreditDecisionEndpoint
{
    private const int MinimumApprovalScore = 50;
    private static readonly (int MinScore, int MaxScore) ManualReviewScoreRange = (50, 60);

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
            var (decision, maxCreditAmount) = DecideOnCreditRequest(customerScore, employmentHistory, request.RequestedCreditAmount);
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

    private static (CreditRequestDecision Decision, decimal MaxCreditAmount)  DecideOnCreditRequest(int score, EmploymentHistoryDto employmentHistory,decimal requestedAmount)
    {
        if (score < MinimumApprovalScore)
        {
            return (CreditRequestDecision.Rejected, 0);
        }

        var maxCreditAmount = CalculateMaxCreditAmount(score, employmentHistory);
        if (requestedAmount > maxCreditAmount)
        {
            return (CreditRequestDecision.Rejected, maxCreditAmount);
        }

        return IsManualReviewRequired(score)
            ? (CreditRequestDecision.ManualReviewRequired, maxCreditAmount)
            : (CreditRequestDecision.Approved, maxCreditAmount);
    }

    private static decimal CalculateMaxCreditAmount(int score, EmploymentHistoryDto employmentHistory) => score switch
    {
        >= 80 => employmentHistory.CurrentNetMonthlyIncome * 20,
        >= 70 and < 80 => employmentHistory.CurrentNetMonthlyIncome * 10,
        >= 60 and < 70 => employmentHistory.CurrentNetMonthlyIncome * 5,
        >= 50 and < 60 => employmentHistory.CurrentNetMonthlyIncome * 3,
        < 50 => 0,
    };

    private static bool IsManualReviewRequired(int score) =>
        score >= ManualReviewScoreRange.MinScore && score <= ManualReviewScoreRange.MaxScore;

    private static string MapScoringDecision(CreditRequestDecision scoringResultDecision) => scoringResultDecision switch
    {
        CreditRequestDecision.Approved => "Approved",
        CreditRequestDecision.ManualReviewRequired => "For manual review",
        CreditRequestDecision.Rejected => "Rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(scoringResultDecision)),
    };
}
