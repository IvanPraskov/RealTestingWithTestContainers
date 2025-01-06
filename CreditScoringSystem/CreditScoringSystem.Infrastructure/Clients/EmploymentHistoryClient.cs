﻿using CreditScoringSystem.Domain.Contracts;
using System.Net.Http.Json;

namespace CreditScoringSystem.Infrastructure.Clients;

internal class EmploymentHistoryClient : IEmploymentHistoryClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EmploymentHistoryClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<EmploymentHistoryResponse?> GetCustomerEmploymentHistory(string customerId, CancellationToken ct)
    {
        var httpClient = _httpClientFactory.CreateClient("EmploymentHistory");

        var response = await httpClient.GetAsync($"api/employmentHistory/{customerId}/", ct);

        return await response.Content.ReadFromJsonAsync<EmploymentHistoryResponse>(ct);
    }
}
