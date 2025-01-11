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
        // Setup
        const decimal requestedAmount = 10000;
        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;

        var empHistoryResponse = new EmploymentHistoryResponse(60, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 0, ExistingMonthlyDebt: 500);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);

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

    /// <summary>
    /// Scenario:
    ///     Employment - less than 1 year on the job.
    ///     Credit history - 2 missed payments
    ///     DTI ratio is 30-60%
    ///     Expected outcome is, appproved for max credit amount 10x on net monthly income, final score of 70.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario2()
    {
        // Setup
        const decimal requestedAmount = 10000;
        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;

        var empHistoryResponse = new EmploymentHistoryResponse(10, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 2, ExistingMonthlyDebt: 2700);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);

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
        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        const decimal requestedAmount = expectedMaxCreditAmount * 2;

        var empHistoryResponse = new EmploymentHistoryResponse(10, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 2, ExistingMonthlyDebt: 2700);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);


        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "Rejected", expectedMaxCreditAmount);

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
        const decimal customerNetMonthlyIncome = 5000;
        const decimal requestedAmount = 20_000;
        // Rejected credit request
        const decimal expectedMaxCreditAmount = 0;
        var empHistoryResponse = new EmploymentHistoryResponse(30, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 4, ExistingMonthlyDebt: 3200);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1975, 06, 02), creditHistoryTestData);

        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "Rejected", expectedMaxCreditAmount);

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
        const int expectedCustomerScore = 40;
        Assert.Equal(expectedCustomerScore, customerScore);
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
        const decimal customerNetMonthlyIncome = 2500;
        const decimal expectedMaxCreditAmount = 3 * customerNetMonthlyIncome;
        const decimal requestedAmount = 7000;

        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 4, ExistingMonthlyDebt: 1500);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(1985, 06, 02), creditHistoryTestData);
        var expectedCreditDecision = new CreditRequestDecisionResponse(testData.CustomerId, "For manual review", expectedMaxCreditAmount);

        var empHistoryResponse = new EmploymentHistoryResponse(10, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

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
        const int expectedCustomerScore = 50;
        Assert.Equal(expectedCustomerScore, customerScore);
    }
}
