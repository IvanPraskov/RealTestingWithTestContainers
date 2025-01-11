using Bogus;
using CreditScoringSystem.Application.Responses;
using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;
using Moq;

namespace CreditScoringSystem.UnitTests.BadTests;
public class CreditRequestTests
{
    [Fact]
    public void BadTest_TestingHardcodedValues()
    {
        var customerId = new Faker().Random.String2(10, "0123456789");
        CreditRequest credit = new(customerId, requestedAmount: 10_000, new(12, 5534));

        Assert.Equal(10_000, credit.RequestedAmount);
        Assert.NotEmpty(credit.CustomerId);
    }

    [Fact]
    public async Task BadTest_NeverFailingTest()
    {
        var customerId = new Faker().Random.String2(10, "0123456789");
        var response = new CreditRequestDecisionResponse(customerId, "Approved", 10000);
        CreditRequest credit = new(customerId, requestedAmount: 10_000, new(12, 5534));
        CreditHistoryDto creditHistory = new(customerId, 0, 500);
        var creditHistoryRepositoryMock = new Mock<ICreditHistoryRepository>();
        creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);
        var creditRequestRepositoryMock = new Mock<ICreditRequestRepository>();
        creditRequestRepositoryMock
            .Setup(x => x.SaveCreditRequest(It.IsAny<CreditRequest>()))
            .Returns(Task.CompletedTask);

        await credit.MakeCreditDecision(creditHistoryRepositoryMock.Object, creditRequestRepositoryMock.Object);

        Assert.NotNull(response);
    }

    [Fact]
    public async Task BadTest_CheckThatAnExceptionIsThrown()
    {
        var customerId = new Faker().Random.String2(10, "0123456789");
        var response = new CreditRequestDecisionResponse(customerId, "Approved", 10000);
        CreditRequest credit = new(customerId, requestedAmount: 10_000, new(12, 5534));
        CreditHistoryDto creditHistory = new(customerId, 0, 500);
        var creditHistoryRepositoryMock = new Mock<ICreditHistoryRepository>();
        creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ThrowsAsync(new Exception());
        var creditRequestRepositoryMock = new Mock<ICreditRequestRepository>();
        creditRequestRepositoryMock
            .Setup(x => x.SaveCreditRequest(It.IsAny<CreditRequest>()))
            .Returns(Task.CompletedTask);

       await Assert.ThrowsAsync<Exception>(async () => await credit.MakeCreditDecision(creditHistoryRepositoryMock.Object, creditRequestRepositoryMock.Object));
    }
}
