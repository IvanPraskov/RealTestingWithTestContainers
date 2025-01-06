using Bogus;
using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;
using Moq;

namespace CreditScoringSystem.UnitTests;

public class CreditRequestTests
{
    private readonly Mock<ICreditHistoryRepository> _creditHistoryRepositoryMock;
    private readonly Mock<ICreditRequestRepository> _creditRequestRepositoryMock;

    public CreditRequestTests()
    {
        _creditHistoryRepositoryMock = new();
        _creditRequestRepositoryMock = new();
        _creditRequestRepositoryMock
            .Setup(x => x.SaveCreditRequest(It.IsAny<CreditRequest>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeApproved_For20xOnMonthlyIncome_WithScoreOf100_WhenFullTimeEmploymentIsOver3Years_CreditHistoryHasNoMissedPayments_AndDebtToIncomeIsLowest()
    {
        const decimal netMonhtlyIncome = 5534;
        const decimal expectedMaxCreditAmount = 20 * netMonhtlyIncome;
        var customerId = new Faker().Random.String2(10, "0123456789");
        CreditRequest credit = new(customerId, requestedAmount: 10_000, customerAge: 27, new(EmploymentType.SelfEmployed, 12, netMonhtlyIncome));
        CreditHistoryDto creditHistory = new(customerId, 0, 500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);
        await credit.MakeCreditDecision(_creditHistoryRepositoryMock.Object, _creditRequestRepositoryMock.Object);

        Assert.Equal(100, credit.CustomerScore);
        Assert.Equal(expectedMaxCreditAmount, credit.MaxCreditAmount);
        Assert.Equal(CreditRequestDecision.Approved, credit.ScoringDecision);
    }
}
