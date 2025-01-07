using CreditScoringSystem.API.CreditRequests.Data.Dtos;
using CreditScoringSystem.API.CreditRequests.Data.Responses;
using CreditScoringSystem.API.CreditRequests.MakeCreditDecision;
using CreditScoringSystem.IntegrationTests.Fixtures;
using CreditScoringSystem.IntegrationTests.Helpers;
using Dapper;
using Moq;
using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CreditScoringSystem.IntegrationTests;

[Collection(nameof(DatabaseFixture))]
public class CreditRequestScoringTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _customWebApplicationFactory;
    private readonly DatabaseFixture _databaseFixture;

    public CreditRequestScoringTests(CustomWebApplicationFactory customWebApplicationFactory, DatabaseFixture databaseFixture)
    {
        _customWebApplicationFactory = customWebApplicationFactory;
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task CreditRequest_ShouldBeApproved_For20xOnMonthlyIncome_WithScoreOf100_WhenFullTimeEmploymentIsOver3Years_CreditHistoryHasNoMissedPayments_AndDebtToIncomeIsLowest()
    {
        // Setup
        const decimal requestedAmount = 10000;
        const decimal customerNetMonthlyIncome = 5000;
        // Max employment stability bonus - over 5 years on full time job
        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.FullTime, 60, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);
        
        // No missed payments, low DTI
        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 0, ExistingMonthlyDebt: 500);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);
        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "Approved", expectedMaxCreditAmount);       

        var httpClient = _customWebApplicationFactory.CreateClient();
        var creditRequest = new CustomerCreditRequest(testData.CustomerId, requestedAmount);

        var content = new StringContent(JsonSerializer.Serialize(creditRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("api/credits", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreditRequestDecisionResponse>();
        Assert.Equal(expectedCreditDecision, result);

        const string selectCustomerScoreSql = """
            SELECT CustomerScore
            FROM CreditRequestScoringResults
            WHERE CustomerId = @CustomerId
            """;
        var customerScore = await conn.ExecuteScalarAsync<int>(selectCustomerScoreSql, new { testData.CustomerId });

        const int expectedCustomerScore = 100;
        Assert.Equal(expectedCustomerScore, customerScore);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeApproved_For10xOnMonthlyIncome_WithScoreOf70_WhenFullTimeEmploymentIsLessThanOneYear_CreditHistoryHasOneOrTwoMissedPayments_AndDebtToIncomeIsMidToHigh()
    {
        // Setup
        const decimal requestedAmount = 10000;
        const decimal customerNetMonthlyIncome = 5000;
       
        // No employment stability bonus because of just 10 months on the full-time job.
        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.FullTime, 10, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);
        
        // High DTI with 2 missed payments -> DTI (50-60%) penalty of 20 and credit history penalty of 10 => -30 score
        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 2, ExistingMonthlyDebt: 2700);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "Approved", expectedMaxCreditAmount);
       

        var httpClient = _customWebApplicationFactory.CreateClient();
        var creditRequest = new CustomerCreditRequest(testData.CustomerId, requestedAmount);
        var content = new StringContent(JsonSerializer.Serialize(creditRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("api/credits", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreditRequestDecisionResponse>();
        Assert.Equal(expectedCreditDecision, result);

        var customerScore = await conn.ExecuteScalarAsync<int>("""
            SELECT CustomerScore
            FROM CreditRequestScoringResults
            WHERE CustomerId = @CustomerId
            """, new { testData.CustomerId });
        const int expectedCustomerScore = 70;
        Assert.Equal(expectedCustomerScore, customerScore);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeRejected_WhenRequestedAmount_IsOverMaxCreditAmount()
    {
        // No employment stability bonus because of just 10 months on the full-time job.
        const decimal customerNetMonthlyIncome = 5000;
        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.FullTime, 10, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // High DTI with 2 missed payments -> DTI (50-60%) penalty of 20 and credit history penalty of 10 => -30 score
        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 2, ExistingMonthlyDebt: 2700);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);
        // Score of 70 will result in 10x max credit amount
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        
        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "Rejected", expectedMaxCreditAmount);

        var httpClient = _customWebApplicationFactory.CreateClient();
        // Set requested amount larger than the expected approved max credit amount
        const decimal requestedAmount = expectedMaxCreditAmount * 2;
        var creditRequest = new CustomerCreditRequest(testData.CustomerId, requestedAmount);
        var content = new StringContent(JsonSerializer.Serialize(creditRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("api/credits", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreditRequestDecisionResponse>();
        Assert.Equal(expectedCreditDecision, result);

        var customerScore = await conn.ExecuteScalarAsync<int>("""
            SELECT CustomerScore
            FROM CreditRequestScoringResults
            WHERE CustomerId = @CustomerId
            """, new { testData.CustomerId });
        const int expectedCustomerScore = 70;
        Assert.Equal(expectedCustomerScore, customerScore);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeRejected_WithScoreOf45_WhenDebToIncomeIsBetween60And70_OverThreeMissedPayments_AndWithPartTimeEmploymentStabilityBonus()
    {
        // Part-time employment stability bonus of +5 because of over 24 months on the job.
        const decimal customerNetMonthlyIncome = 5000;
        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.PartTime, 30, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // High DTI with 4 missed payments -> DTI (60-70%) penalty of 30 and credit history penalty of 30 => -60 score
        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 4, ExistingMonthlyDebt: 3200);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);
        // Rejected credit request
        const decimal expectedMaxCreditAmount = 0;

        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "Rejected", expectedMaxCreditAmount);

        var httpClient = _customWebApplicationFactory.CreateClient();
        // Set requested amount larger than the expected approved max credit amount
        const decimal requestedAmount = 20_000;
        var creditRequest = new CustomerCreditRequest(testData.CustomerId, requestedAmount);
        var content = new StringContent(JsonSerializer.Serialize(creditRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("api/credits", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreditRequestDecisionResponse>();
        Assert.Equal(expectedCreditDecision, result);

        var customerScore = await conn.ExecuteScalarAsync<int>("""
            SELECT CustomerScore
            FROM CreditRequestScoringResults
            WHERE CustomerId = @CustomerId
            """, new { testData.CustomerId });
        const int expectedCustomerScore = 45;
        Assert.Equal(expectedCustomerScore, customerScore);
    }

    [Fact]
    public async Task CreditRequest_ShouldBeApproved_ForCustomerAgedUnder25_For20xOnMonthlyIncome_WithScoreOf95_WhenDebToIncomeIsBetween60And70_NoCreditHistory_AndNoPartTimeEmploymentStabilityBonus()
    {
        // No Part-time employment stability bonus because of less than 24 months on the job.
        const decimal customerNetMonthlyIncome = 1500;
        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.PartTime, 12, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // No credit history and under 25 -> -5 
        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 0, ExistingMonthlyDebt: 0, IsWithCreditHistory: false);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var under25BirthYear = DateTime.UtcNow.Year - 23;
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(under25BirthYear, 06, 02), creditHistoryTestData);

        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;
        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "Approved", expectedMaxCreditAmount);

        var httpClient = _customWebApplicationFactory.CreateClient();
        const decimal requestedAmount = 20_000;
        var creditRequest = new CustomerCreditRequest(testData.CustomerId, requestedAmount);
        var content = new StringContent(JsonSerializer.Serialize(creditRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("api/credits", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreditRequestDecisionResponse>();
        Assert.Equal(expectedCreditDecision, result);

        var customerScore = await conn.ExecuteScalarAsync<int>("""
            SELECT CustomerScore
            FROM CreditRequestScoringResults
            WHERE CustomerId = @CustomerId
            """, new { testData.CustomerId });
        const int expectedCustomerScore = 95;
        Assert.Equal(expectedCustomerScore, customerScore);
    }
}
