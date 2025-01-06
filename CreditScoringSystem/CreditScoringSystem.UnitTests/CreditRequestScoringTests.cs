using Bogus;
using CreditScoringSystem.Application;
using CreditScoringSystem.Application.Responses;
using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;
using Microsoft.Extensions.Logging;
using Moq;

namespace CreditScoringSystem.UnitTests;
public class CreditRequestScoringTests
{
    private readonly CreditRequestService _sut;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IEmploymentHistoryClient> _employmentHistoryClientMock;
    private readonly Mock<ICreditHistoryRepository> _creditHistoryRepositoryMock;
    private readonly Mock<ICreditRequestRepository> _creditRequestRepositoryMock;
    private readonly Mock<ILogger<CreditRequestService>> _loggerMock;

    public CreditRequestScoringTests()
    {
        _customerRepositoryMock = new();
        _employmentHistoryClientMock = new();
        _creditHistoryRepositoryMock = new();
        _creditRequestRepositoryMock = new();
        _creditRequestRepositoryMock
            .Setup(x => x.SaveCreditRequest(It.IsAny<CreditRequest>()))
            .Returns(Task.CompletedTask);
        _loggerMock = new();
        _sut = new(_customerRepositoryMock.Object, _employmentHistoryClientMock.Object, _creditHistoryRepositoryMock.Object, _creditRequestRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeApproved_For20xOnMonthlyIncome_WithScoreOf100_WhenFullTimeEmploymentIsOver3Years_CreditHistoryHasNoMissedPayments_AndDebtToIncomeIsLowest()
    {
        Customer customer = new()
        {
            Age = 30,
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        // Max employment stability bonus - over 5 years on full time job
        const decimal customerNetMonthlyIncome = 5000;
        var empHistoryResponse = new EmploymentHistoryResponse(Domain.EmploymentType.FullTime, 60, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 0, 500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Approved", expectedMaxCreditAmount);

        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);

        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 100)), Times.Once);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeApproved_For10xOnMonthlyIncome_WithScoreOf70_WhenFullTimeEmploymentIsLessThanOneYear_CreditHistoryHasOneOrTwoMissedPayments_AndDebtToIncomeIsMidToHigh()
    {
        Customer customer = new()
        {
            Age = 30,
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        // Max employment stability bonus - over 5 years on full time job
        const decimal customerNetMonthlyIncome = 5000;
        // No employment stability bonus because of just 10 months on the full-time job.
        var empHistoryResponse = new EmploymentHistoryResponse(Domain.EmploymentType.FullTime, 10, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // High DTI with 2 missed payments -> DTI (50-60%) penalty of 20 and credit history penalty of 10 => -30 score
        CreditHistoryDto creditHistory = new(customer.CustomerId, 2, 2700);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Approved", expectedMaxCreditAmount);

        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);

        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 70)), Times.Once);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeRejected_WhenRequestedAmount_IsOverMaxCreditAmount()
    {
        Customer customer = new()
        {
            Age = 30,
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        // Max employment stability bonus - over 5 years on full time job
        const decimal customerNetMonthlyIncome = 5000;
        // No employment stability bonus because of just 10 months on the full-time job.
        var empHistoryResponse = new EmploymentHistoryResponse(Domain.EmploymentType.FullTime, 10, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // High DTI with 2 missed payments -> DTI (50-60%) penalty of 20 and credit history penalty of 10 => -30 score
        CreditHistoryDto creditHistory = new(customer.CustomerId, 2, 2700);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Rejected", expectedMaxCreditAmount);
        // Set requested amount larger than the expected approved max credit amount
        const decimal requestedAmount = expectedMaxCreditAmount * 2;
        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, requestedAmount), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);

        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 70)), Times.Once);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeRejected_WithScoreOf45_WhenDebToIncomeIsBetween60And70_OverThreeMissedPayments_AndWithPartTimeEmploymentStabilityBonus()
    {
        Customer customer = new()
        {
            Age = 30,
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        // Part-time employment stability bonus of +5 because of over 24 months on the job.
        const decimal customerNetMonthlyIncome = 5000;
        var empHistoryResponse = new EmploymentHistoryResponse(Domain.EmploymentType.PartTime, 30, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // High DTI with 4 missed payments -> DTI (60-70%) penalty of 30 and credit history penalty of 30 => -60 score
        CreditHistoryDto creditHistory = new(customer.CustomerId, 4, 3200);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);
        // Rejected
        const decimal expectedMaxCreditAmount = 0;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Rejected", expectedMaxCreditAmount);
        // Set requested amount larger than the expected approved max credit amount
        const decimal requestedAmount = 10_000;
        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, requestedAmount), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);

        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 45)), Times.Once);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeApproved_ForCustomerAgedUnder25_For20xOnMonthlyIncome_WithScoreOf95_WhenDebToIncomeIsBetween60And70_NoCreditHistory_AndNoPartTimeEmploymentStabilityBonus()
    {
        Customer customer = new()
        {
            Age = 23,
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        // No Part-time employment stability bonus because of less than 24 months on the job.
        const decimal customerNetMonthlyIncome = 1500;
        var empHistoryResponse = new EmploymentHistoryResponse(Domain.EmploymentType.PartTime, 12, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // No credit history and under 25 -> -5 
        CreditHistoryDto? creditHistory = null;
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);
        // Rejected
        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Approved", expectedMaxCreditAmount);
        // Set requested amount larger than the expected approved max credit amount
        const decimal requestedAmount = 10_000;
        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, requestedAmount), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);

        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 95)), Times.Once);
    }
}
