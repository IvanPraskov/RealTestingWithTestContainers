using Microsoft.Extensions.DependencyInjection;

namespace CreditScoringSystem.Application.Configuration.DI;

public static class CompositionRootExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services.AddTransient<ICreditRequestService, CreditRequestService>();
    }
}
