using Bogus;
using CreditScoringSystem.Application;
using CreditScoringSystem.Application.Responses;
using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;
using Microsoft.Extensions.Logging;
using Moq;

namespace CreditScoringSystem.UnitTests.BadTests;
public class CreditRequestServiceTests
{
    private readonly Application.CreditRequestService _sut;
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

    [Fact]
    public async Task BadTest_CheckingReturnValue_IsNotNull()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 5000;
        var empHistoryResponse = new EmploymentHistoryResponse(60, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 0, 500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);
        Assert.NotNull(actualCreditDecision);
    }

    [Fact]
    public async Task BadTest_CheckingReturnValue_IsTypeOf()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 5000;
        var empHistoryResponse = new EmploymentHistoryResponse(60, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 0, 500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);
        Assert.IsType<CreditRequestDecisionResponse>(actualCreditDecision);
    }

    [Fact]
    public async Task BadTest_MeaninglessBoundaryTesting()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 5000;
        var empHistoryResponse = new EmploymentHistoryResponse(60, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 0, 500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        var actualCreditDecision = await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);
        Assert.NotNull(actualCreditDecision);
        Assert.True(actualCreditDecision!.MaxCreditAmount > 0);
        Assert.NotEmpty(actualCreditDecision!.CreditRequestDecision);
    }

    [Fact]
    public async Task BadTest_TestingDependencyCalls()
    {
        Customer customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
        };
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        const decimal customerNetMonthlyIncome = 5000;
        EmploymentHistoryResponse empHistoryResponse = new (60, customerNetMonthlyIncome);
        _employmentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryDto creditHistory = new(customer.CustomerId, 0, 500);
        _creditHistoryRepositoryMock
            .Setup(x => x.GetCustomerCreditHistory(It.IsAny<string>()))
            .ReturnsAsync(creditHistory);

        await _sut.MakeCreditRequestDecision(new(customer.CustomerId, 10_000), CancellationToken.None);

        _customerRepositoryMock.Verify(x => x.GetCustomerById(It.IsAny<string>()), Times.Once);
        _employmentHistoryClientMock.Verify(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _creditHistoryRepositoryMock.Verify(x => x.GetCustomerCreditHistory(It.IsAny<string>()), Times.Once);
        _creditRequestRepositoryMock.Verify(x => x.SaveCreditRequest(It.IsAny<CreditRequest>()), Times.Once);
    }


    [Fact]
    public async Task BadTest_CheckingReturnValue_CouplingToCurrentImplementation()
    {
        Customer? customer = null;
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        var response = await _sut.MakeCreditRequestDecision(new("12345123", 10_000), CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task BadTest_CheckingLogMessages()
    {
        Customer? customer = null;
        _customerRepositoryMock
            .Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync(customer);

        await _sut.MakeCreditRequestDecision(new("12345123", 10_000), CancellationToken.None);

        _loggerMock.Verify(x =>
            x.Log(LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No customer found for command")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
    }
}
