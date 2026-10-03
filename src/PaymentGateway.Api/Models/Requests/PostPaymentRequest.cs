namespace PaymentGateway.Api.Models.Requests;

/// <summary>
/// Card number and CVV are strings so leading zeros survive and 19-digit card numbers fit.
/// Every field is nullable so a missing value can be reported as "required" rather than defaulting to 0.
/// </summary>
public class PostPaymentRequest
{
    public string? CardNumber { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? Currency { get; set; }
    public int? Amount { get; set; }
    public string? Cvv { get; set; }
}
