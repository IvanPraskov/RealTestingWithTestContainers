using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Infrastructure.Clients;
using CreditScoringSystem.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScoringSystem.Infrastructure.Configuration.DI;
public static class CompositionRootExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .AddTransient<ICustomerRepository, CustomerRepository>()
            .AddTransient<ICreditHistoryRepository, CreditHistoryRepository>()
            .AddTransient<ICreditRequestRepository, CreditRequestRepository>();
    }

    public static IServiceCollection AddClients(this IServiceCollection services)
    {
        return services.AddScoped<IEmploymentHistoryClient, EmploymentHistoryClient>();
    }
}
