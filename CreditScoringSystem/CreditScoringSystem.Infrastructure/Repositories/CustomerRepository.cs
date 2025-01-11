using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Infrastructure.DbModels;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreditScoringSystem.Infrastructure.Repositories;

internal class CustomerRepository : ICustomerRepository
{
    private readonly string _connString;

    public CustomerRepository(IConfiguration configuration)
    {
        _connString = configuration.GetConnectionString("CreditScoringSystem") ?? string.Empty;
    }

    public async Task<Customer?> GetCustomerById(string customerId)
    {
        await using var conn = new NpgsqlConnection(_connString);
        const string sql = """
            SELECT *
            FROM Customers
            WHERE CustomerId = @CustomerId
            """;

        var result = await conn.QuerySingleOrDefaultAsync<CustomerDbModel>(sql, new { CustomerId = customerId });

        return result is null
            ? null
            : new()
            {
                CustomerId = customerId
            };
    }
}
