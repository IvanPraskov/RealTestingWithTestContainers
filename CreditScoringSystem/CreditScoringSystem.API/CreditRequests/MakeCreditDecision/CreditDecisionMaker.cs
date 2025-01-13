namespace CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

internal static class CreditDecisionMaker
{
    private const int MinimumApprovalScore = 50;
    private static readonly (int MinScore, int MaxScore) ManualReviewScoreRange = (50, 60);

    public static (CreditRequestDecision CreditDecision, decimal MaxCreditAmount) Decide(int customerScore, decimal requestedAmount, decimal netMonthlyIncome)
    {
        if (customerScore < MinimumApprovalScore)
        {
            return (CreditRequestDecision.Rejected, 0);
        }

        var maxCreditAmount = CalculateMaxCreditAmount(customerScore, netMonthlyIncome);
        if (requestedAmount > maxCreditAmount)
        {
            return (CreditRequestDecision.Rejected, maxCreditAmount);
        }

        return IsManualReviewRequired(customerScore)
            ? (CreditRequestDecision.ManualReviewRequired, maxCreditAmount)
            : (CreditRequestDecision.Approved, maxCreditAmount);
    }

    private static decimal CalculateMaxCreditAmount(int score, decimal currentNetMonthlyIncome) => score switch
    {
        >= 80 => currentNetMonthlyIncome * 20,
        >= 70 and < 80 => currentNetMonthlyIncome * 10,
        >= 60 and < 70 => currentNetMonthlyIncome * 5,
        >= 50 and < 60 => currentNetMonthlyIncome * 3,
        < 50 => 0,
    };

    private static bool IsManualReviewRequired(int score) =>
        score >= ManualReviewScoreRange.MinScore && score <= ManualReviewScoreRange.MaxScore;
}
