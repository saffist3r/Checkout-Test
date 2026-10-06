namespace PaymentGateway.Api.Models;

/// <summary>
/// Outcome of a payment that reached the acquiring bank. These are the only statuses a stored payment can have.
/// Invalid requests never reach the bank; they get a <see cref="Responses.RejectedPaymentResponse"/> instead.
/// </summary>
public enum PaymentStatus
{
    Authorized,
    Declined
}
