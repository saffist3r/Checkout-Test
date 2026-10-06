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
    private readonly PaymentMetrics _metrics;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        PaymentRequestValidator validator,
        PaymentsService paymentsService,
        PaymentsRepository paymentsRepository,
        PaymentMetrics metrics,
        ILogger<PaymentsController> logger)
    {
        _validator = validator;
        _paymentsService = paymentsService;
        _paymentsRepository = paymentsRepository;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Processes a card payment through the acquiring bank.
    /// </summary>
    /// <remarks>
    /// With the bank simulator, the card's last digit decides the outcome: odd is Authorized, even is Declined, 0 means the bank is unavailable.
    /// </remarks>
    /// <response code="200">The bank answered. The status is Authorized or Declined, and the payment is stored.</response>
    /// <response code="400">The request is invalid. Every field error is returned, the bank is not called and nothing is stored.</response>
    /// <response code="502">The bank is unavailable, so the outcome is unknown. Nothing is stored and the request can be retried.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RejectedPaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> PostPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = _validator.Validate(request);
        if (errors.Count > 0)
        {
            _metrics.RecordPayment(PaymentMetrics.Rejected);
            return BadRequest(new RejectedPaymentResponse(errors));
        }

        try
        {
            Payment payment = await _paymentsService.ProcessAsync(request, cancellationToken);
            _logger.LogInformation("Payment {PaymentId} processed with status {Status}", payment.Id, payment.Status);
            _metrics.RecordPayment(payment.Status.ToString());
            return Ok(PaymentResponse.From(payment));
        }
        catch (BankUnavailableException ex)
        {
            _metrics.RecordPayment(PaymentMetrics.BankUnavailable);
            _logger.LogWarning(ex, "Acquiring bank unavailable (bank status {BankStatusCode})", (int?)ex.HttpStatusCode);
            return Problem(
                title: "The acquiring bank is unavailable. The payment was not processed, please retry later.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Retrieves a previously processed payment.
    /// </summary>
    /// <param name="id">The id returned when the payment was processed.</param>
    /// <response code="200">The payment, with only the last four card digits.</response>
    /// <response code="404">No payment exists with this id.</response>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
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
