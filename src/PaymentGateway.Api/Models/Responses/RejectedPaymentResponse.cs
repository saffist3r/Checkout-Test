namespace PaymentGateway.Api.Models.Responses;

/// <summary>
/// Returned when the gateway refuses a request before calling the bank. No payment is created, so there is no id.
/// </summary>
public record RejectedPaymentResponse(IDictionary<string, string[]> Errors)
{
    public string Status => "Rejected";
}
