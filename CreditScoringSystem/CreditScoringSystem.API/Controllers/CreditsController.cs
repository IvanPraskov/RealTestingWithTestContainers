using Microsoft.AspNetCore.Mvc;

namespace CreditScoringSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreditsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ScoreClientCreditRequest(ClientCreditRequest request)
    {
        return Ok();
    }

    public record ClientCreditRequest(string ClientId, decimal RequestedCreditAmount, decimal DeclaredMonthlyIncome, decimal DeclaredExistingDebts, EmploymentStatus EmploymentStatus);
}

public enum EmploymentStatus 
{
    FullTime = 1,
    PartTime = 2,
    SelfEmployeed = 3,
}