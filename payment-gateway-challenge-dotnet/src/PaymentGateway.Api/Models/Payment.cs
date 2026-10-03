namespace PaymentGateway.Api.Models;

/// <summary>
/// A payment that was sent to the acquiring bank. Only the last four card digits are kept:
/// the full card number and CVV are never stored.
/// </summary>
public record Payment(
    Guid Id,
    PaymentStatus Status,
    string CardNumberLastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string? AuthorizationCode);
