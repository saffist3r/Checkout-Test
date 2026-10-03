using System.Text.Json;

namespace PaymentGateway.Api.Services.Bank;

public class AcquiringBankClient : IAcquiringBankClient
{
    private const string PaymentsPath = "payments";

    private readonly HttpClient _httpClient;

    public AcquiringBankClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(PaymentsPath, request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new BankUnavailableException("Could not reach the acquiring bank.", innerException: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BankUnavailableException("The acquiring bank timed out.", innerException: ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new BankUnavailableException(
                    $"The acquiring bank returned {(int)response.StatusCode}.",
                    response.StatusCode);
            }

            try
            {
                BankPaymentResponse? body = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken);
                return body ?? throw new BankUnavailableException("The acquiring bank returned an empty response.", response.StatusCode);
            }
            catch (JsonException ex)
            {
                throw new BankUnavailableException("The acquiring bank returned an unreadable response.", response.StatusCode, ex);
            }
        }
    }
}
