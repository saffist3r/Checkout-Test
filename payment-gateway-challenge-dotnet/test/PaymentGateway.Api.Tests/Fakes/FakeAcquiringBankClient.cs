using PaymentGateway.Api.Services.Bank;

namespace PaymentGateway.Api.Tests.Fakes;

/// <summary>
/// Mirrors the simulator's rules: odd last digit authorizes, even declines, zero is unavailable.
/// </summary>
public class FakeAcquiringBankClient : IAcquiringBankClient
{
    public List<BankPaymentRequest> Requests { get; } = new();

    public Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        int lastDigit = request.CardNumber[^1] - '0';
        if (lastDigit == 0)
        {
            throw new BankUnavailableException("The acquiring bank returned 503.");
        }

        return Task.FromResult(lastDigit % 2 == 1
            ? new BankPaymentResponse(true, Guid.NewGuid().ToString())
            : new BankPaymentResponse(false, string.Empty));
    }
}
