using CreditScoringSystem.API.Configuration.DI;
using CreditScoringSystem.Application.Configuration.DI;
using CreditScoringSystem.Infrastructure;
using CreditScoringSystem.Infrastructure.Configuration.DI;

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
            .AddApplicationServices()
            .AddRepositories()
            .AddClients();

        var app = builder.Build();

        await new DbSeeder(app.Configuration).Seed();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
