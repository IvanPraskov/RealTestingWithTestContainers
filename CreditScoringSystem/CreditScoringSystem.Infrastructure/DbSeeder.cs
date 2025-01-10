using Bogus;
using CreditScoringSystem.Infrastructure.DbModels;
using Dapper;
using Npgsql;
using System.Data;
using System.Transactions;

namespace CreditScoringSystem.Infrastructure;

public class DbSeeder
{
    public static async Task Seed(string connString)
    {
        await using var conn = new NpgsqlConnection(connString);

        await InitializeDbSchema(conn);
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

    private static async Task InitializeDbSchema(NpgsqlConnection conn)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS public.customers
            (
                customerid character(10) COLLATE pg_catalog."default" NOT NULL,
                dateofbirth date NOT NULL,
                customerfirstname character varying(50) COLLATE pg_catalog."default" NOT NULL,
                customermiddlename character varying(50) COLLATE pg_catalog."default",
                customerlastname character varying(50) COLLATE pg_catalog."default" NOT NULL,
                CONSTRAINT customers_pkey PRIMARY KEY (customerid)
            );

            CREATE TABLE IF NOT EXISTS public.creditrequestdecisions
            (
                creditrequestdecisionid integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
                description text COLLATE pg_catalog."default" NOT NULL,
                CONSTRAINT creditrequestdecisions_pkey PRIMARY KEY (creditrequestdecisionid)
            );
            

            CREATE TABLE IF NOT EXISTS public.creditrequestscoringresults
            (
                creditrequestscoringresultid integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
                customerid character(10) COLLATE pg_catalog."default" NOT NULL,
                requestedamount numeric(14,2) NOT NULL,
                creditrequestdecisionid integer NOT NULL,
                customerscore integer NOT NULL,
                createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
                updatedat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
                maxcreditamount numeric(14,2) NOT NULL DEFAULT 0,
                CONSTRAINT creditrequestscoringresults_pkey PRIMARY KEY (creditrequestscoringresultid),
                CONSTRAINT creditrequestscoringresults_creditrequestdecision_fkey FOREIGN KEY (creditrequestdecisionid)
                    REFERENCES public.creditrequestdecisions (creditrequestdecisionid) MATCH SIMPLE
                    ON UPDATE NO ACTION
                    ON DELETE NO ACTION
                    NOT VALID,
                CONSTRAINT creditrequestscoringresults_creditrequestdecisions_fkey FOREIGN KEY (creditrequestdecisionid)
                    REFERENCES public.creditrequestdecisions (creditrequestdecisionid) MATCH SIMPLE
                    ON UPDATE NO ACTION
                    ON DELETE NO ACTION
                    NOT VALID,
                CONSTRAINT creditrequestscoringresults_customerid_fkey FOREIGN KEY (customerid)
                    REFERENCES public.customers (customerid) MATCH SIMPLE
                    ON UPDATE NO ACTION
                    ON DELETE NO ACTION
            );

            CREATE TABLE IF NOT EXISTS public.credithistories
            (
                customerid character(10) COLLATE pg_catalog."default" NOT NULL,
                missedpayments integer NOT NULL DEFAULT 0,
                existingmonthlydebt numeric(14,2),
                CONSTRAINT credithistories_pkey PRIMARY KEY (customerid),
                CONSTRAINT credithistories_customerid_fkey FOREIGN KEY (customerid)
                    REFERENCES public.customers (customerid) MATCH SIMPLE
                    ON UPDATE NO ACTION
                    ON DELETE NO ACTION
            );
            """;

        await conn.ExecuteAsync(sql);
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
