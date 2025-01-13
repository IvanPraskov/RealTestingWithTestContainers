using CreditScoringSystem.API.CreditRequests.Data.Dtos;

namespace CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

internal static class CreditScoreCalculator
{
    private const int MaxCustomerScore = 100;

    public static int Calculate(CreditHistoryDto? creditHistory, EmploymentHistoryDto employmentHistory)
    {
        var score = MaxCustomerScore;
        score = ApplyCreditHistoryPenaltyIfAny(score, creditHistory);
        score = ApplyDebtToIncomePenaltyIfAny(score, creditHistory?.ExistingMonthlyDebt ?? 0, employmentHistory);
        score = ApplyEmploymentStabilityBonusIfAny(score, employmentHistory);

        return Math.Clamp(score, 0, MaxCustomerScore);
    }

    private static int ApplyCreditHistoryPenaltyIfAny(int currentScore, CreditHistoryDto? creditHistory)
    {
        currentScore -= creditHistory?.MissedPayments switch
        {
            0 => 0,
            null => 5,
            1 or 2 => 10,
            >= 3 => 30,
            _ => throw new ArgumentOutOfRangeException(nameof(creditHistory)),
        };

        return currentScore;
    }

    private static int ApplyDebtToIncomePenaltyIfAny(int currentScore, decimal existingDebt, EmploymentHistoryDto employmentHistory)
    {
        var dti = Math.Round((existingDebt / employmentHistory.CurrentNetMonthlyIncome) * 100, MidpointRounding.ToNegativeInfinity);
        currentScore -= dti switch
        {
            <= 30 => 0,
            > 30 and <= 60 => 20,
            > 60 => 40,
        };


        return currentScore;
    }

    private static int ApplyEmploymentStabilityBonusIfAny(int currentScore, EmploymentHistoryDto employmentHistory)
    {
        currentScore += employmentHistory.EmploymentDurationInMonths switch
        {
            >= 60 => 15,
            >= 12 and < 60 => 10,
            < 12 => 0,
        };

        return currentScore;
    }
}
