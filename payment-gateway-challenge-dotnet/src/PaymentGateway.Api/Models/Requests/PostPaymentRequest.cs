namespace PaymentGateway.Api.Models.Requests;

/// <summary>
/// Card number and CVV are strings so leading zeros survive and 19-digit card numbers fit.
/// Every field is nullable so a missing value can be reported as "required" rather than defaulting to 0.
/// </summary>
public class PostPaymentRequest
{
    /// <summary>Card number, 14 to 19 digits.</summary>
    /// <example>2222405343248877</example>
    public string? CardNumber { get; set; }

    /// <summary>Expiry month, 1 to 12.</summary>
    /// <example>4</example>
    public int? ExpiryMonth { get; set; }

    /// <summary>Expiry year, four digits. Month and year must not be in the past.</summary>
    /// <example>2030</example>
    public int? ExpiryYear { get; set; }

    /// <summary>Uppercase ISO 4217 code. Supported: GBP, USD, EUR.</summary>
    /// <example>GBP</example>
    public string? Currency { get; set; }

    /// <summary>Amount in minor units, greater than zero. 1050 means 10.50.</summary>
    /// <example>1050</example>
    public int? Amount { get; set; }

    /// <summary>CVV, 3 or 4 digits. Never stored.</summary>
    /// <example>123</example>
    public string? Cvv { get; set; }
}
