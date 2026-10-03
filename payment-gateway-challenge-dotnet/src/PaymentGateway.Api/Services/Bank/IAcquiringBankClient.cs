namespace PaymentGateway.Api.Services.Bank;

public interface IAcquiringBankClient
{
    /// <exception cref="BankUnavailableException">The bank did not return an authorization decision.</exception>
    Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request, CancellationToken cancellationToken = default);
}
