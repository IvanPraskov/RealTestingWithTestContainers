using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;

namespace CreditScoringSystem.Domain;

public sealed class CreditRequest
{
    public int CustomerScore { get; private set; }

    public decimal MaxCreditAmount { get; private set; }

    public CreditRequestDecision CreditRequestDecision { get; private set;  }

    public string CustomerId { get; }

    public decimal RequestedAmount { get; }

    private const int MaxCustomerScore = 100;

    private readonly EmploymentHistoryDto _employmentHistory;

    private readonly int _customerAge;

    private readonly (int MinScore, int MaxScore) ManualReviewScoreRange = (50, 60);

    private const int MinimumApprovalScore = 50;

    public CreditRequest(string customerId, decimal requestedAmount, int customerAge, EmploymentHistoryDto employmentHistory)
    {
        CustomerId = customerId;
        RequestedAmount = requestedAmount;
        _customerAge = customerAge;
        _employmentHistory = employmentHistory;
    }

    public async Task MakeCreditDecision(ICreditHistoryRepository creditHistoryRepository, ICreditRequestRepository creditRequestRepository)
    {
        CustomerScore = await CalculateCreditScore(creditHistoryRepository);
        CreditRequestDecision = DecideOnCreditRequest(CustomerScore);

        await creditRequestRepository.SaveCreditRequest(this);
    }

    private async Task<int> CalculateCreditScore(ICreditHistoryRepository creditHistoryRepository)
    {
        var score = MaxCustomerScore;
        var creditHistory = await creditHistoryRepository.GetCustomerCreditHistory(CustomerId);
        score = ApplyCreditHistoryPenaltyIfAny(score, creditHistory);
        score = ApplyDebtToIncomePenaltyIfAny(score, creditHistory?.ExistingMonthlyDebt ?? 0);
        score = ApplyEmploymentStabilityBonusIfAny(score);

        return Math.Clamp(score, 0, MaxCustomerScore);
    }

    private int ApplyCreditHistoryPenaltyIfAny(int currentScore, CreditHistoryDto? creditHistory)
    {
        if (creditHistory is null && _customerAge < 25)
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

    private int ApplyDebtToIncomePenaltyIfAny(int currentScore, decimal existingDebt)
    {
        var dti = CalculateDebtToIncomeRatio(existingDebt, _employmentHistory.CurrentNetMonthlyIncome);
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

    private static decimal CalculateDebtToIncomeRatio(decimal existingDebt, decimal currentNetMonthlyIncome)
        => Math.Round((existingDebt / currentNetMonthlyIncome) * 100, MidpointRounding.ToNegativeInfinity);

    private int ApplyEmploymentStabilityBonusIfAny(int currentScore)
    {
        currentScore += _employmentHistory.EmploymentType switch
        {
            EmploymentType.SelfEmployed => _employmentHistory.EmploymentDurationInMonths switch
            {
                >= 60 => 15,
                >= 24 and < 60 => 10,
                < 24 => 0,
            },
            EmploymentType.FullTime => _employmentHistory.EmploymentDurationInMonths switch
            {
                >= 36 => 10,
                >= 12 and < 36 => 5,
                < 12 => 0,
            },
            EmploymentType.PartTime => _employmentHistory.EmploymentDurationInMonths switch
            {
                >= 24 => 5,
                < 24 => 0,
            },
            _ => throw new ArgumentOutOfRangeException(_employmentHistory.EmploymentType.ToString()),
        };

        return currentScore;
    }


    private CreditRequestDecision DecideOnCreditRequest(int score)
    {
        if (score < MinimumApprovalScore)
        {
            return CreditRequestDecision.Rejected;
        }

        MaxCreditAmount = CalculateMaxCreditAmount(score);
        if (RequestedAmount > MaxCreditAmount)
        {
            return CreditRequestDecision.Rejected;
        }

        return IsManualReviewRequired(score)
            ? CreditRequestDecision.ManualReviewRequired
            : CreditRequestDecision.Approved;
    }

    private decimal CalculateMaxCreditAmount(int score) => score switch
    {
        >= 80 => _employmentHistory.CurrentNetMonthlyIncome * 20,
        >= 70 and < 80 => _employmentHistory.CurrentNetMonthlyIncome * 10,
        >= 60 and < 70 => _employmentHistory.CurrentNetMonthlyIncome * 5,
        >= 50 and < 60 => _employmentHistory.CurrentNetMonthlyIncome * 3,
        < 50 => 0,
    };

    private bool IsManualReviewRequired(int score) =>
        score >= ManualReviewScoreRange.MinScore && score <= ManualReviewScoreRange.MaxScore;
}
