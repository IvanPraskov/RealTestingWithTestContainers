using CreditScoringSystem.API.Configuration.DI;
using CreditScoringSystem.API.CreditRequests;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CreditScoringSystem.IntegrationTests")]
[assembly: InternalsVisibleTo("CreditScoringSystem.UnitTests")]
namespace CreditScoringSystem.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        builder.Services
            .AddOpenApi()
            .AddHttpClients(builder.Configuration)
            .AddCreditRequestModule();

        var app = builder.Build();

        var connString = app.Configuration.GetConnectionString("CreditScoringSystem") ?? string.Empty;
        await DbSeeder.Seed(connString);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapCreditRequests();

        app.Run();
    }
}
