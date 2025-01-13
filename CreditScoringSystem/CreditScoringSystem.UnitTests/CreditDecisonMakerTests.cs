using CreditScoringSystem.API.CreditRequests.MakeCreditDecision;

namespace CreditScoringSystem.UnitTests;
public class CreditDecisonMakerTests
{

    /// <summary>
    /// Scenario:
    ///     Score above 80, requested amount lower than max credit amount
    ///     Expected result - Approved for 20x on monthly income
    /// </summary>
    [Fact]
    public void CreditDecisionMaker_Scenario1()
    {
        var expectedDecision = CreditRequestDecision.Approved;
        const int customerScore = 80;
        const decimal requestedAmount = 20_000;
        const decimal netMonthlyIncome = 7500;

        var (actualDecision, maxCreditAmount) = CreditDecisionMaker.Decide(customerScore, requestedAmount, netMonthlyIncome);

        Assert.Equal(expectedDecision, actualDecision);
        Assert.Equal(netMonthlyIncome * 20, maxCreditAmount);
    }

    /// <summary>
    /// Scenario:
    ///     Score below 50
    ///     Expected result - Rejected, max credit amount 0
    /// </summary>
    [Fact]
    public void CreditDecisionMaker_Scenario2()
    {
        var expectedDecision = CreditRequestDecision.Rejected;
        const int customerScore = 49;
        const decimal requestedAmount = 20_000;
        const decimal netMonthlyIncome = 7500;

        var (actualDecision, maxCreditAmount) = CreditDecisionMaker.Decide(customerScore, requestedAmount, netMonthlyIncome);

        Assert.Equal(expectedDecision, actualDecision);
        Assert.Equal(0, maxCreditAmount);
    }

    /// <summary>
    /// Scenario:
    ///     Score between 50-60
    ///     Expected result - Manual review required, max credit amount 3x net monthly income
    /// </summary>
    [Fact]
    public void CreditDecisionMaker_Scenario3()
    {
        var expectedDecision = CreditRequestDecision.ManualReviewRequired;
        const int customerScore = 53;
        const decimal requestedAmount = 12_000;
        const decimal netMonthlyIncome = 7500;

        var (actualDecision, maxCreditAmount) = CreditDecisionMaker.Decide(customerScore, requestedAmount, netMonthlyIncome);

        Assert.Equal(expectedDecision, actualDecision);
        Assert.Equal(3 * netMonthlyIncome, maxCreditAmount);
    }

    /// <summary>
    /// Scenario:
    ///     Score between 60-70
    ///     Expected result - Approved, max credit amount 5x net monthly income
    /// </summary>
    [Fact]
    public void CreditDecisionMaker_Scenario4()
    {
        var expectedDecision = CreditRequestDecision.Approved;
        const int customerScore = 61;
        const decimal requestedAmount = 12_000;
        const decimal netMonthlyIncome = 7500;

        var (actualDecision, maxCreditAmount) = CreditDecisionMaker.Decide(customerScore, requestedAmount, netMonthlyIncome);

        Assert.Equal(expectedDecision, actualDecision);
        Assert.Equal(5 * netMonthlyIncome, maxCreditAmount);
    }

    /// <summary>
    /// Scenario:
    ///     Score between 70-80
    ///     Expected result - Approved, max credit amount 10x net monthly income
    /// </summary>
    [Fact]
    public void CreditDecisionMaker_Scenario5()
    {
        var expectedDecision = CreditRequestDecision.Approved;
        const int customerScore = 71;
        const decimal requestedAmount = 12_000;
        const decimal netMonthlyIncome = 7500;

        var (actualDecision, maxCreditAmount) = CreditDecisionMaker.Decide(customerScore, requestedAmount, netMonthlyIncome);

        Assert.Equal(expectedDecision, actualDecision);
        Assert.Equal(10 * netMonthlyIncome, maxCreditAmount);
    }

    /// <summary>
    /// Scenario:
    ///     Any score between 50-100
    ///     Expected result - Rejected, requested amount higher than max credit amount
    /// </summary>
    [Theory]
    [InlineData(50)]
    [InlineData(55)]
    [InlineData(64)]
    [InlineData(74)]
    [InlineData(82)]
    public void CreditDecisionMaker_Scenario6(int customerScore)
    {
        var expectedDecision = CreditRequestDecision.Rejected;
        const decimal requestedAmount = 100_000;
        const decimal netMonthlyIncome = 3000;

        var (actualDecision, _) = CreditDecisionMaker.Decide(customerScore, requestedAmount, netMonthlyIncome);

        Assert.Equal(expectedDecision, actualDecision);
    }
}
