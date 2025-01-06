using CreditScoringSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace CreditScoringSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreditsController : ControllerBase
{
    private readonly ICreditRequestService _creditRequestScoringService;

    public CreditsController(ICreditRequestService creditRequestScoringService)
    {
        _creditRequestScoringService = creditRequestScoringService;
    }

    [HttpPost]
    public async Task<IActionResult> ScoreClientCreditRequest(CustomerCreditRequest request, CancellationToken ct)
    {
        if (request.CustomerId.Length != 10 || request.RequestedCreditAmount <= 0)
        {
            return BadRequest();
        }

        var response = await _creditRequestScoringService.MakeCreditRequestDecision(new(request.CustomerId, request.RequestedCreditAmount), ct);
        return response is null
            ? BadRequest()
            : Ok(response);
    }

    
}
public record CustomerCreditRequest(string CustomerId, decimal RequestedCreditAmount);