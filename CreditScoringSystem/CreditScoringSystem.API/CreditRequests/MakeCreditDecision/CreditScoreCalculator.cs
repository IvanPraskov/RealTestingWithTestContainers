using CreditScoringSystem.API.CreditRequests.Data.Dtos;

namespace CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

public static class CreditScoreCalculator
{
    private const int MaxCustomerScore = 100;

    public static int Calculate(CreditHistoryDto? creditHistory, EmploymentHistoryDto employmentHistory, int customerAge)
    {
        var score = MaxCustomerScore;
        score = ApplyCreditHistoryPenaltyIfAny(score, creditHistory, customerAge);
        score = ApplyDebtToIncomePenaltyIfAny(score, creditHistory?.ExistingMonthlyDebt ?? 0, employmentHistory);
        score = ApplyEmploymentStabilityBonusIfAny(score, employmentHistory);

        return Math.Clamp(score, 0, MaxCustomerScore);
    }

    private static int ApplyCreditHistoryPenaltyIfAny(int currentScore, CreditHistoryDto? creditHistory, int customerAge)
    {
        if (creditHistory is null && customerAge < 25)
        {
            currentScore -= 5;
        }

        currentScore -= creditHistory?.MissedPayments switch
        {
            0 or null => 0,
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
            > 30 and <= 50 => 10,
            > 50 and <= 60 => 20,
            > 60 and <= 70 => 30,
            > 70 => 40,
        };

        return currentScore;
    }

    private static int ApplyEmploymentStabilityBonusIfAny(int currentScore, EmploymentHistoryDto employmentHistory)
    {
        currentScore += employmentHistory.EmploymentType switch
        {
            EmploymentType.SelfEmployed => employmentHistory.EmploymentDurationInMonths switch
            {
                >= 60 => 15,
                >= 24 and < 60 => 10,
                < 24 => 0,
            },
            EmploymentType.FullTime => employmentHistory.EmploymentDurationInMonths switch
            {
                >= 36 => 10,
                >= 12 and < 36 => 5,
                < 12 => 0,
            },
            EmploymentType.PartTime => employmentHistory.EmploymentDurationInMonths switch
            {
                >= 24 => 5,
                < 24 => 0,
            },
            _ => throw new ArgumentOutOfRangeException(employmentHistory.EmploymentType.ToString()),
        };

        return currentScore;
    }
}
