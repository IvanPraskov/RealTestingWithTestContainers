using CreditScoringSystem.API.CreditRequests.Data.Dtos;
using CreditScoringSystem.API.Extensions;
using Dapper;
using Npgsql;

namespace CreditScoringSystem.API.CreditRequests.Data.Database;

internal class CreditRequestPersistence : ICreditRequestPersistance
{
    private readonly string _connString;

    public CreditRequestPersistence(IConfiguration configuration)
    {
        _connString = configuration.GetConnectionString("CreditScoringSystem") ?? string.Empty;
    }

    public async Task<CustomerDto?> GetCustomerById(string customerId)
    {
        await using var conn = new NpgsqlConnection(_connString);
        const string sql = """
            SELECT DateOfBirth
            FROM Customers
            WHERE CustomerId = @CustomerId
            """;

        var result = await conn.QuerySingleOrDefaultAsync<CustomerDbModel>(sql, new { CustomerId = customerId });

        return result is null
            ? null
            : new()
            {
                CustomerId = customerId,
                Age = result.DateOfBirth.GetAge(),
            };
    }

    public async Task SaveCreditRequest(CreditRequestDbModel creditRequest)
    {
        await using var conn = new NpgsqlConnection(_connString);

        const string sql = """
        INSERT INTO public.creditrequestscoringresults(
        customerid, requestedamount, creditrequestdecisionid, customerscore, maxcreditamount)
        VALUES (@CustomerId, @RequestedAmount, @CreditRequestDecisionId, @CustomerScore, @MaxCreditAmount);
        """;

        await conn.ExecuteAsync(sql, creditRequest);
    }

    public async Task<CreditHistoryDto?> GetCustomerCreditHistory(string customerId)
    {
        await using var conn = new NpgsqlConnection(_connString);
        const string sql = """
            SELECT MissedPayments, ExistingMonthlyDebt
            FROM CreditHistories
            WHERE CustomerId = @CustomerId
            """;

        var result = await conn.QuerySingleOrDefaultAsync<CreditHistoryDbModel>(sql, new { CustomerId = customerId });
        return result is null
            ? null
            : new(customerId, result.MissedPayments, result.ExistingMonthlyDebt);
    }
}
