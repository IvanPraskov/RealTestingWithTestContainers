using CreditScoringSystem.API;
using CreditScoringSystem.API.CreditRequests.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CreditScoringSystem.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _databaseFixture;
    public Mock<IEmploymentHistoryClient> EmploymentHistoryClientMock { get; } = new Mock<IEmploymentHistoryClient>();

    public CustomWebApplicationFactory(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    ["ConnectionStrings:CreditScoringSystem"] = _databaseFixture.ConnectionString,
                });
            })
            .ConfigureTestServices(services =>
            {
                // Replace employment history client dependency with a mocked one,
                // so that whenever needed the mocked instance can be retrieved and setup for each individual test.
                services.AddScoped(_ => EmploymentHistoryClientMock.Object);
            });
    }
}
