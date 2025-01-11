namespace CreditScoringSystem.API.Extensions;
public static class DateTimeExtensions
{
    public static int GetAge(this DateTime dateOfBirth)
    {
        var now = DateTime.UtcNow;
        int age = now.Year - dateOfBirth.Year;
        if (now.Month < dateOfBirth.Month || now.Month == dateOfBirth.Month && now.Day < dateOfBirth.Day)
        {
            age--;
        }

        return age;
    }
}
