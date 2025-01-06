namespace CreditScoringSystem.API.Configuration.DI;

public static class CompositionRootExtensions
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("EmploymentHistory", httpClient =>
        {
            var empHistoryApiBaseAddress = configuration["EmploymentHistoryApi:ApiBaseAddress"] ?? string.Empty;
            httpClient.BaseAddress = new Uri(empHistoryApiBaseAddress);
        });

        return services;
    }
}
