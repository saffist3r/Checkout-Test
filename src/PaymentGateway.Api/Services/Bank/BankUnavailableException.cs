using System.Net;

namespace PaymentGateway.Api.Services.Bank;

/// <summary>
/// The acquiring bank could not give an authorization decision (error status, network failure or unreadable response).
/// </summary>
public sealed class BankUnavailableException : Exception
{
    public BankUnavailableException(string message, HttpStatusCode? httpStatusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>The bank's HTTP status, or null when no response was received.</summary>
    public HttpStatusCode? HttpStatusCode { get; }
}
