using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;
using CreditScoringSystem.Infrastructure.DbModels;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreditScoringSystem.Infrastructure.Repositories;

internal class CreditHistoryRepository : ICreditHistoryRepository
{
    private readonly string _connString;

    public CreditHistoryRepository(IConfiguration configuration)
    {
        _connString = configuration.GetConnectionString("CreditScoringSystem") ?? string.Empty;
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
