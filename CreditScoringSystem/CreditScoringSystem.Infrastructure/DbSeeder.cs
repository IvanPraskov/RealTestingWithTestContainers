using Bogus;
using CreditScoringSystem.Infrastructure.DbModels;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using System.Transactions;

namespace CreditScoringSystem.Infrastructure;

public class DbSeeder
{
    private readonly string _connString;

    public DbSeeder(IConfiguration configuration)
    {
        _connString = configuration.GetConnectionString("CreditScoringSystem") ?? string.Empty;
    }

    public async Task Seed()
    {
        await using var conn = new NpgsqlConnection(_connString);

        const string queryExistingRecords = """
            SELECT COUNT(*) 
            FROM Customers c
            JOIN CreditHistories ch
            ON c.CustomerId = ch.CustomerId
            """;
        var existingRecordCount = await conn.ExecuteScalarAsync<int>(queryExistingRecords);
        // Records already seeded
        if (existingRecordCount > 0)
        {
            return;
        }

        var transactionOption = new TransactionOptions()
        {
            IsolationLevel = System.Transactions.IsolationLevel.Snapshot,
        };
        using var transaction = new TransactionScope(TransactionScopeOption.Required, transactionOption, TransactionScopeAsyncFlowOption.Enabled);
        await SeedCustomers(conn);
        await SeedCreditHistory(conn);
        await SeedCreditRequestDecisions(conn);
        transaction.Complete();
    }

    private static async Task SeedCustomers(IDbConnection conn)
    {
        IReadOnlyList<CustomerDbModel> customers = [
            // customer under 25
            new()
            {
                CustomerId = "0141260470",
                DateOfBirth = new DateTime(new DateOnly(2001, 01, 26), TimeOnly.MinValue, DateTimeKind.Utc),
                CustomerFirstName = new Faker().Name.FirstName(),
                CustomerMiddleName = new Faker().Name.LastName(),
                CustomerLastName = new Faker().Name.LastName(),
            },
            new()
            {
                CustomerId = "9001013400",
                DateOfBirth = new DateTime (new DateOnly(1990, 01, 28), TimeOnly.MinValue, DateTimeKind.Utc),
                CustomerFirstName = new Faker().Name.FirstName(),
                CustomerMiddleName = new Faker().Name.LastName(),
                CustomerLastName = new Faker().Name.LastName(),
            },
            new()
            {
                CustomerId = "8403162283",
                DateOfBirth = new DateTime (new DateOnly(1984, 03, 16), TimeOnly.MinValue, DateTimeKind.Utc),
                CustomerFirstName = new Faker().Name.FirstName(),
                CustomerMiddleName = new Faker().Name.LastName(),
                CustomerLastName = new Faker().Name.LastName(),
            },
            new()
            {
                CustomerId = "7506027756",
                DateOfBirth = new DateTime (new DateOnly(1975, 06, 02), TimeOnly.MinValue, DateTimeKind.Utc),
                CustomerFirstName = new Faker().Name.FirstName(),
                CustomerMiddleName = new Faker().Name.LastName(),
                CustomerLastName = new Faker().Name.LastName(),
            },
        ];

        const string insertScript = """
            INSERT INTO public.customers(
            customerid, dateofbirth, customerfirstname, customermiddlename, customerlastname)
            VALUES (@CustomerId, @DateOfBirth, @CustomerFirstName, @CustomerMiddleName, @CustomerLastName);
            """;
        await conn.ExecuteAsync(insertScript, customers);
    }

    private static async Task SeedCreditHistory(IDbConnection conn)
    {
        IReadOnlyList<CreditHistoryDbModel> creditHistories = [
            new()
            {
                CustomerId = "0141260470",
                MissedPayments = 0,
            },
             new()
            {
                CustomerId = "9001013400",
                MissedPayments = 2,
            },
             new()
            {
                CustomerId = "8403162283",
                MissedPayments = 1,
            },
            new()
            {
                CustomerId = "7506027756",
                MissedPayments = 0,
            }
        ];

        const string insertScript = """
            INSERT INTO public.credithistories(
            customerid, missedpayments)
            VALUES (@CustomerId, @MissedPayments);
            """;
        await conn.ExecuteAsync(insertScript, creditHistories);
    }

    private static async Task SeedCreditRequestDecisions(IDbConnection conn)
    {
        const string insertScript = """
            INSERT INTO public.creditrequestdecisions(
            description)
            VALUES ('ManualReviewRequired'), ('Rejected'), ('Approved');
            """;
        await conn.ExecuteAsync(insertScript);
    }
}
