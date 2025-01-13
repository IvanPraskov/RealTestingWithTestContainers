using CreditScoringSystem.API.CreditRequests.Data.Dtos;
using CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

namespace CreditScoringSystem.UnitTests;
public class CreditScoreCalculatorTests
{

    /// <summary>
    /// Scenario:
    ///     EmploymentType - Over 3 years on the job. +10
    ///     Credit history - no missed payments. -0
    ///     DTI ratio is below 30%. -0
    ///     Expected Score of 100.
    /// </summary>
    [Fact]
    public void CreditScoreCalculator_Scenario1()
    {
        const int expectedScore = 100;
        EmploymentHistoryDto employmentHistory = new(60, 5000);
        CreditHistoryDto creditHistory = new("123", 0, 500);

        var actualScore = CreditScoreCalculator.Calculate(creditHistory, employmentHistory);

        Assert.Equal(expectedScore, actualScore);
    }

    /// <summary>
    /// Scenario:
    ///     Employment - less than 1 year on the job.
    ///     Credit history - 2 missed payments
    ///     DTI ratio is 30-60%
    ///     Expected Score of 70.
    /// </summary>
    [Fact] 
    public void CreditScoreCalculator_Scenario2()
    {
        const int expectedScore = 70;
        EmploymentHistoryDto employmentHistory = new(7, 2500);
        CreditHistoryDto creditHistory = new("123", 2, 1250);

        var actualScore = CreditScoreCalculator.Calculate(creditHistory, employmentHistory);

        Assert.Equal(expectedScore, actualScore);
    }

    /// <summary>
    /// Scenario:
    ///     Employment - over 2 years on the job, +10
    ///     Credit history - 4 missed payments, -30
    ///     DTI ratio is over 60%, -40
    ///     Expected Score of 40.
    /// </summary>
    [Fact]
    public void CreditScoreCalculator_Scenario3()
    {
        const int expectedScore = 40;
        EmploymentHistoryDto employmentHistory = new(27, 3000);
        CreditHistoryDto creditHistory = new("123", 4, 2000);

        var actualScore = CreditScoreCalculator.Calculate(creditHistory, employmentHistory);

        Assert.Equal(expectedScore, actualScore);
    }

    /// <summary>
    /// Scenario
    ///     EmploymentType - less than an year on the job, +0
    ///     Credit history - over 3 missed payments, -30
    ///     DTI - 30-60%, -20
    ///     Expected outcome is, manual review required for max credit amount 3x on net monthly income, final score of 50.
    /// </summary>
    [Fact]
    public void CreditScoreCalculator_Scenario4()
    {
        const int expectedScore = 50;

        EmploymentHistoryDto employmentHistory = new(10, 4000);
        CreditHistoryDto creditHistory = new("123", 5, 1700);

        var actualScore = CreditScoreCalculator.Calculate(creditHistory, employmentHistory);

        Assert.Equal(expectedScore, actualScore);
    }
}
