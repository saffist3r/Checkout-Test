using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services.Bank;

namespace PaymentGateway.Api.Services;

public class PaymentsService
{
    private readonly IAcquiringBankClient _bankClient;
    private readonly PaymentsRepository _paymentsRepository;

    public PaymentsService(IAcquiringBankClient bankClient, PaymentsRepository paymentsRepository)
    {
        _bankClient = bankClient;
        _paymentsRepository = paymentsRepository;
    }

    /// <summary>
    /// Sends an already validated request to the acquiring bank and stores the outcome.
    /// </summary>
    /// <exception cref="BankUnavailableException">The bank did not return a decision; nothing is stored.</exception>
    public async Task<Payment> ProcessAsync(PostPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        BankPaymentRequest bankRequest = new(
            request.CardNumber!,
            $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            request.Currency!,
            request.Amount!.Value,
            request.Cvv!);

        BankPaymentResponse bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest, cancellationToken);

        Payment payment = new(
            Guid.NewGuid(),
            bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            request.CardNumber![^4..],
            request.ExpiryMonth!.Value,
            request.ExpiryYear!.Value,
            request.Currency!,
            request.Amount.Value,
            bankResponse.AuthorizationCode);

        _paymentsRepository.Add(payment);
        return payment;
    }
}
