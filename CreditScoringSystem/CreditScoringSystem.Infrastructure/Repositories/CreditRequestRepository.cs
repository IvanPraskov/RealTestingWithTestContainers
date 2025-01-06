using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Infrastructure.DbModels;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreditScoringSystem.Infrastructure.Repositories;

internal class CreditRequestRepository : ICreditRequestRepository
{
    private readonly string _connString;

    public CreditRequestRepository(IConfiguration configuration)
    {
        _connString = configuration.GetConnectionString("CreditScoringSystem") ?? string.Empty;
    }

    public async Task SaveCreditRequest(CreditRequest creditRequest)
    {
        await using var conn = new NpgsqlConnection(_connString);
        CreditRequestDbModel dbModel = new()
        {
            CustomerId = creditRequest.CustomerId,
            RequestedAmount = creditRequest.RequestedAmount,
            MaxCreditAmount = creditRequest.MaxCreditAmount,
            CreditRequestDecisionId = creditRequest.ScoringDecision,
            CustomerScore = creditRequest.CustomerScore,
        };

        const string sql = """
        INSERT INTO public.creditrequestscoringresults(
        customerid, requestedamount, creditrequestdecisionid, customerscore, maxcreditamount)
        VALUES (@CustomerId, @RequestedAmount, @CreditRequestDecisionId, @CustomerScore, @MaxCreditAmount);
        """;

        await conn.ExecuteAsync(sql, dbModel);
    }
}
