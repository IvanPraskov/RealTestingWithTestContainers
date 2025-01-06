using Dapper;
using Npgsql;
using System.Data;

namespace CreditScoringSystem.IntegrationTests;
internal static class DatabaseInitializer
{
    public static async Task Initialize(string connString)
    {
        await using var conn = new NpgsqlConnection(connString);
        await IniitalizeDbSchema(conn);
        await AddCreditRequestDecisions(conn);
    }

    private static async Task IniitalizeDbSchema(IDbConnection conn)
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

    private static async Task AddCreditRequestDecisions(IDbConnection conn)
    {
        const string insertScript = """
            INSERT INTO public.creditrequestdecisions(
            description)
            VALUES ('ManualReviewRequired'), ('Rejected'), ('Approved');
            """;
        await conn.ExecuteAsync(insertScript);
    }
}
