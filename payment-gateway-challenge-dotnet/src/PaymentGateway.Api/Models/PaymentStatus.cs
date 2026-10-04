namespace PaymentGateway.Api.Models;

/// <summary>
/// The three statuses from the brief. Stored payments are only ever Authorized or Declined:
/// Rejected is returned for invalid requests, which never reach the bank and are not stored.
/// </summary>
public enum PaymentStatus
{
    Authorized,
    Declined,
    Rejected
}
