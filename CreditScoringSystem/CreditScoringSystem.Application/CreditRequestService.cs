using CreditScoringSystem.Application.Commands;
using CreditScoringSystem.Application.Responses;
using CreditScoringSystem.Domain;
using CreditScoringSystem.Domain.Contracts;
using CreditScoringSystem.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace CreditScoringSystem.Application;

internal class CreditRequestService : ICreditRequestService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmploymentHistoryClient _employmentHistoryClient;
    private readonly ICreditHistoryRepository _creditHistoryRepository;
    private readonly ICreditRequestRepository _creditRequestRepository;
    private readonly ILogger<CreditRequestService> _logger;

    public CreditRequestService(ICustomerRepository customerRepository,
        IEmploymentHistoryClient employmentHistoryClient,
        ICreditHistoryRepository creditHistoryRepository,
        ICreditRequestRepository creditRequestRepository,
        ILogger<CreditRequestService> logger)
    {
        _customerRepository = customerRepository;
        _employmentHistoryClient = employmentHistoryClient;
        _creditHistoryRepository = creditHistoryRepository;
        _creditRequestRepository = creditRequestRepository;
        _logger = logger;
    }

    // TODO: Return OperationResult to allow for more complex handling of different error cases
    public async Task<CreditRequestDecisionResponse?> MakeCreditRequestDecision(ScoreCreditRequest command, CancellationToken ct)
    {
        var customer = await _customerRepository.GetCustomerById(command.CustomerId);
        if (customer is null)
        {
            _logger.LogWarning("No customer found for command {@Command}", command);
            return null;
        }

        var emplHistoryResponse = await _employmentHistoryClient.GetCustomerEmploymentHistory(command.CustomerId, ct);
        if (emplHistoryResponse is null)
        {
            _logger.LogWarning("No employment history found for command {@Command}", command);
            return null;
        }

        EmploymentHistoryDto empHistoryDto =
            new(emplHistoryResponse.EmploymentDurationInMonths, emplHistoryResponse.CurrentNetMonthlyIncome);
        var creditRequest = new CreditRequest(command.CustomerId, command.RequestedAmount, empHistoryDto);

        await creditRequest.MakeCreditDecision(_creditHistoryRepository, _creditRequestRepository);
        _logger.LogInformation("Credit request scored successfully. {@CreditRequest}", creditRequest);
        return new(creditRequest.CustomerId, MapCreditRequestDecision(creditRequest.CreditRequestDecision), creditRequest.MaxCreditAmount);
    }

    private static string MapCreditRequestDecision(CreditRequestDecision scoringResultDecision) => scoringResultDecision switch
    {
        CreditRequestDecision.Approved => "Approved",
        CreditRequestDecision.ManualReviewRequired => "For manual review",
        CreditRequestDecision.Rejected => "Rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(scoringResultDecision)),
    };
}
