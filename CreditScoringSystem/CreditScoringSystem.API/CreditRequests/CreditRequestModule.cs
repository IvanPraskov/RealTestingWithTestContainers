using CreditScoringSystem.API.CreditRequests.Clients;
using CreditScoringSystem.API.CreditRequests.Data.Database;

namespace CreditScoringSystem.API.CreditRequests;

internal static class CreditRequestModule
{
    internal static IServiceCollection AddCreditRequestModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IEmploymentHistoryClient, EmploymentHistoryClient>()
            .AddTransient<ICreditRequestPersistance, CreditRequestPersistence>();
    }
}
