using Bogus;
using CreditScoringSystem.Application;
using CreditScoringSystem.Application.Responses;
using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;
using Microsoft.Extensions.Logging;
using Moq;

namespace CreditScoringSystem.UnitTests;
public class CreditRequestServiceTests
{
    private readonly CreditRequestService _sut;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IEmploymentHistoryClient> _employmentHistoryClientMock;
    private readonly Mock<ICreditHistoryRepository> _creditHistoryRepositoryMock;
    private readonly Mock<ICreditRequestRepository> _creditRequestRepositoryMock;
    private readonly Mock<ILogger<CreditRequestService>> _loggerMock;

    public CreditRequestServiceTests()
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

    /// <summary>
    /// Scenario:
    ///     EmploymentType - Over 3 years on the job. +10
    ///     Credit history - no missed payments. -0
    ///     DTI ratio is below 30%. -0
    ///     Expected outcome is, appproved for max credit amount 20x on net monthly income, final score of 100.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario1()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        // Max employment stability bonus - over 5 years on full time job
        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Approved", expectedMaxCreditAmount);
        var empHistoryResponse = new EmploymentHistoryResponse(60, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 0, 500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory); 

        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);
        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 100)), Times.Once);
    }

    /// <summary>
    /// Scenario:
    ///     Employment - less than 1 year on the job. +0
    ///     Credit history - 2 missed payments -10
    ///     DTI ratio is 30-60%, -20
    ///     Expected outcome is, appproved for max credit amount 10x on net monthly income, final score of 70.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario2()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Approved", expectedMaxCreditAmount);
        var empHistoryResponse = new EmploymentHistoryResponse(10, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);
        
        CreditHistoryDto creditHistory = new(customer.CustomerId, 2, 2700);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);
        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 70)), Times.Once);
    }

    /// <summary>
    /// Scenario:
    ///     EmploymentType - less than 1 year on the job.
    ///     Credit history - 2 missed payments
    ///     DTI ratio is 50-60%
    ///     Expected outcome is, rejected because requested amount is over max credit amount 10x on net monthly income, final score of 70.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario3()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Rejected", expectedMaxCreditAmount);
        // Set requested amount larger than the expected approved max credit amount
        const decimal requestedAmount = expectedMaxCreditAmount * 2;
        var empHistoryResponse = new EmploymentHistoryResponse(10, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 2, 2700);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);
       
        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, requestedAmount), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);
        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 70)), Times.Once);
    }

    /// <summary>
    /// Scenario:
    ///     Employment - over 2 years on the job, +10
    ///     Credit history - 4 missed payments, -30
    ///     DTI ratio is over 60%, -40
    ///     Expected outcome is, rejected because of low score, final score of 40.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario4()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 5000;
        // Rejected
        const decimal expectedMaxCreditAmount = 0;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "Rejected", expectedMaxCreditAmount);
        const decimal requestedAmount = 10_000;
        var empHistoryResponse = new EmploymentHistoryResponse(30, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 4, 3200);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);     
        
        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, requestedAmount), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);
        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 40)), Times.Once);
    }

    /// <summary>
    /// Scenario
    ///     EmploymentType - less than an year on the job, +0
    ///     Credit history - over 3 missed payments, -30
    ///     DTI - 30-60%, -20
    ///     Expected outcome is, manual review required for max credit amount 3x on net monthly income, final score of 50.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario5()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 2500;
        const decimal expectedMaxCreditAmount = 3 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(customer.CustomerId, "For manual review", expectedMaxCreditAmount);
        const decimal requestedAmount = 7000;

        var empHistoryResponse = new EmploymentHistoryResponse(10, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto? creditHistory = new(customer.CustomerId, 4, 1500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

      
        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, requestedAmount), CancellationToken.None);

        Assert.Equal(expectedCreditDecision, actualCreditDecision);
        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.Is<CreditRequest>(cr => cr.CustomerScore == 50)), Times.Once);
    }
}
