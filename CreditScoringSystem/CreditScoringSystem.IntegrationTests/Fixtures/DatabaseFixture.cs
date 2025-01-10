using CreditScoringSystem.Infrastructure;
using Testcontainers.PostgreSql;

namespace CreditScoringSystem.IntegrationTests.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17.2")
        .Build();
    
    public string ConnectionString { get; private set; } = string.Empty;

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
        await DbSeeder.Seed(ConnectionString);
    } 
}

[CollectionDefinition(nameof(DatabaseFixture))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
