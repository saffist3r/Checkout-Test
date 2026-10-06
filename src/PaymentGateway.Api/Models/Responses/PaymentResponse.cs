namespace PaymentGateway.Api.Models.Responses;

/// <summary>A processed payment as returned to the merchant.</summary>
/// <param name="Id">The payment id, used to retrieve it later.</param>
/// <param name="Status">Authorized or Declined.</param>
/// <param name="CardNumberLastFour">The last four card digits, as a string so leading zeros are kept.</param>
/// <param name="ExpiryMonth">Expiry month, 1 to 12.</param>
/// <param name="ExpiryYear">Expiry year.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="Amount">Amount in minor units.</param>
public record PaymentResponse(
    Guid Id,
    PaymentStatus Status,
    string CardNumberLastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount)
{
    public static PaymentResponse From(Payment payment) => new(
        payment.Id,
        payment.Status,
        payment.CardNumberLastFour,
        payment.ExpiryMonth,
        payment.ExpiryYear,
        payment.Currency,
        payment.Amount);
}
