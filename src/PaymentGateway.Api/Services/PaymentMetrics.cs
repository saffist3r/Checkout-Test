using System.Diagnostics.Metrics;

namespace PaymentGateway.Api.Services;

/// <summary>
/// Counts payment requests by outcome, so a spike in declines, rejections or bank failures is visible.
/// Read it with <c>dotnet-counters monitor --counters PaymentGateway</c>, or any OpenTelemetry exporter.
/// </summary>
public class PaymentMetrics
{
    public const string MeterName = "PaymentGateway";
    public const string Rejected = "Rejected";
    public const string BankUnavailable = "BankUnavailable";

    private readonly Counter<long> _payments;

    public PaymentMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);
        _payments = meter.CreateCounter<long>("payments.processed", description: "Payment requests by outcome.");
    }

    /// <param name="outcome">Authorized, Declined, Rejected or BankUnavailable.</param>
    public void RecordPayment(string outcome) => _payments.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
