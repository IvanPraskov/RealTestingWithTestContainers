
namespace EmploymentHistory.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthorization();

            app.MapGet("/api/employmentHistory/{customerId}", (string customerId) =>
            {
                var empHistories = new Dictionary<string, CustomerEmploymentHistory>()
                {

                    {"0141260470", new("0141260470", EmploymentType.PartTime, 8, 1500) },
                    {"9001013400", new("9001013400", EmploymentType.FullTime, 60, 5000) },
                    {"8403162283", new("8403162283", EmploymentType.SelfEmployed, 24, 4000) },
                    {"7506027756", new("7506027756", EmploymentType.FullTime, 20, 1200) },
                };

                return empHistories.TryGetValue(customerId, out var empHistory)
                ? Results.Ok(empHistory)
                : Results.NoContent();
            });

            app.Run();
        }
    }

    internal record CustomerEmploymentHistory(string CustomerId, EmploymentType EmploymentType, int EmploymentDurationInMonths, decimal CurrentNetMonthlyIncome);
    internal enum EmploymentType
    {
        SelfEmployed = 1,
        PartTime = 2,
        FullTime = 3,
    }
}
