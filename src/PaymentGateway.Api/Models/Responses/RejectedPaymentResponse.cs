namespace PaymentGateway.Api.Models.Responses;

public record RejectedPaymentResponse(IDictionary<string, string[]> Errors)
{
    public PaymentStatus Status => PaymentStatus.Rejected;
}
