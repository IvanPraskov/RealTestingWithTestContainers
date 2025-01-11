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
    ///     EmploymentType - Full-Time, over 3 years on the job.
    ///     Credit history - no missed payments, age over 25.
    ///     DTI ratio is below 30%
    ///     Expected outcome is, appproved for max credit amount 20x on net monthly income, final score of 100.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario1()
    {
        const decimal requestedAmount = 10000;
        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;

        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.FullTime, 60, customerNetMonthlyIncome);
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
    ///     EmploymentType - Full-Time, less than 1 year on the job.
    ///     Credit history - 2 missed payments, age over 25.
    ///     DTI ratio is 50-60%
    ///     Expected outcome is, appproved for max credit amount 10x on net monthly income, final score of 70.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario2()
    {
        const decimal requestedAmount = 10000;
        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;

        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.FullTime, 10, customerNetMonthlyIncome);
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
    ///     EmploymentType - Full-Time, less than 1 year on the job.
    ///     Credit history - 2 missed payments, age over 25.
    ///     DTI ratio is 50-60%
    ///     Expected outcome is, rejected because requested amount is over max credit amount 10x on net monthly income, final score of 70.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario3()
    {
        const decimal customerNetMonthlyIncome = 5000;
        const decimal expectedMaxCreditAmount = 10 * customerNetMonthlyIncome;
        const decimal requestedAmount = expectedMaxCreditAmount * 2;

        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.FullTime, 10, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        // High DTI with 2 missed payments -> DTI (50-60%) penalty of 20 and credit history penalty of 10 => -30 score
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
    ///     EmploymentType - Part-Time, over 2 years on the job.
    ///     Credit history - 4 missed payments, age over 25.
    ///     DTI ratio is 60-70%
    ///     Expected outcome is, rejected because of low score, final score of 45.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario4()
    {
        const decimal customerNetMonthlyIncome = 5000;
        // Rejected credit request
        const decimal expectedMaxCreditAmount = 0;
        const decimal requestedAmount = 20_000;
        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.PartTime, 30, customerNetMonthlyIncome);
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
        const int expectedCustomerScore = 45;
        Assert.Equal(expectedCustomerScore, customerScore);
    }

    /// <summary>
    /// Scenario:
    ///     EmploymentType - Part-Time, less than 2 year on the job.
    ///     Credit history - no credit history, age under 25.
    ///     Expected outcome is, appproved for max credit amount 20x on net monthly income, final score of 95.
    /// </summary>
    [Fact]
    public async Task CreditRequest_Scenario5()
    {
        const decimal customerNetMonthlyIncome = 1500;
        const decimal expectedMaxCreditAmount = 20 * customerNetMonthlyIncome;
        const decimal requestedAmount = 20_000;

        var empHistoryResponse = new EmploymentHistoryResponse(EmploymentType.PartTime, 12, customerNetMonthlyIncome);
        _customWebApplicationFactory.EmploymentHistoryClientMock
            .Setup(x => x.GetCustomerEmploymentHistory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empHistoryResponse);

        CreditHistoryTestData creditHistoryTestData = new(MissedPayments: 0, ExistingMonthlyDebt: 0, IsWithCreditHistory: false);
        await using var conn = new NpgsqlConnection(_databaseFixture.ConnectionString);
        var under25BirthYear = DateTime.UtcNow.Year - 23;
        var testData = await CreditRequestTestHelper.SetupTestData(conn, new DateOnly(under25BirthYear, 06, 02), creditHistoryTestData);
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
        const int expectedCustomerScore = 95;
        Assert.Equal(expectedCustomerScore, customerScore);
    }
}
