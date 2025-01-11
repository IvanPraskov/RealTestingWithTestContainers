using Bogus;
using CreditScoringSystem.API.CreditRequests.Data.Database;
using Dapper;
using System.Data;

namespace CreditScoringSystem.IntegrationTests.Helpers;

internal static class CreditRequestTestHelper
{
    public static async Task<CreditRequestTestData> SetupTestData(IDbConnection conn, DateOnly customerDateOfBirth, CreditHistoryTestData creditHistoryTestData)
    {
        CustomerDbModel customer = new()
        {
            CustomerId = new Faker().Random.String2(10, "0123456789"),
            DateOfBirth = new DateTime(customerDateOfBirth, TimeOnly.MinValue, DateTimeKind.Utc),
            CustomerFirstName = new Faker().Name.FirstName(),
            CustomerMiddleName = new Faker().Name.LastName(),
            CustomerLastName = new Faker().Name.LastName(),
        };

        const string insertCustomer = """
            INSERT INTO public.customers(
            customerid, dateofbirth, customerfirstname, customermiddlename, customerlastname)
            VALUES (@CustomerId, @DateOfBirth, @CustomerFirstName, @CustomerMiddleName, @CustomerLastName);
            """;

        await conn.ExecuteAsync(insertCustomer, customer);

        // There are scenarios where customer might not have credit history.
        if (creditHistoryTestData.IsWithCreditHistory)
        {
            CreditHistoryDbModel creditHistory = new()
            {
                CustomerId = customer.CustomerId,
                MissedPayments = creditHistoryTestData.MissedPayments,
                ExistingMonthlyDebt = creditHistoryTestData.ExistingMonthlyDebt,
            };
            const string insertCrHistory = """
            INSERT INTO public.credithistories(
            customerid, missedpayments, existingmonthlydebt)
            VALUES (@CustomerId, @MissedPayments, @ExistingMonthlyDebt);
            """;

            await conn.ExecuteAsync(insertCrHistory, creditHistory);
        }

        return new(customer.CustomerId);
    }
}

internal record CreditRequestTestData(string CustomerId);

internal record CreditHistoryTestData(int MissedPayments, decimal ExistingMonthlyDebt, bool IsWithCreditHistory = true);
