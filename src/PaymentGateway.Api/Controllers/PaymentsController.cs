using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.Bank;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly PaymentRequestValidator _validator;
    private readonly PaymentsService _paymentsService;
    private readonly PaymentsRepository _paymentsRepository;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        PaymentRequestValidator validator,
        PaymentsService paymentsService,
        PaymentsRepository paymentsRepository,
        ILogger<PaymentsController> logger)
    {
        _validator = validator;
        _paymentsService = paymentsService;
        _paymentsRepository = paymentsRepository;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RejectedPaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> PostPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = _validator.Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(new RejectedPaymentResponse(errors));
        }

        try
        {
            Payment payment = await _paymentsService.ProcessAsync(request, cancellationToken);
            _logger.LogInformation("Payment {PaymentId} processed with status {Status}", payment.Id, payment.Status);
            return Ok(PaymentResponse.From(payment));
        }
        catch (BankUnavailableException ex)
        {
            _logger.LogWarning(ex, "Acquiring bank unavailable (bank status {BankStatusCode})", (int?)ex.HttpStatusCode);
            return Problem(
                title: "The acquiring bank is unavailable. The payment was not processed, please retry later.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PaymentResponse> GetPayment(Guid id)
    {
        Payment? payment = _paymentsRepository.Get(id);
        if (payment is null)
        {
            return NotFound();
        }

        return PaymentResponse.From(payment);
    }
}
