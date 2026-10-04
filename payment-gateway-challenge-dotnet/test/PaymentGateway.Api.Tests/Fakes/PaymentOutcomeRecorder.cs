using System.Diagnostics.Metrics;

namespace PaymentGateway.Api.Tests.Fakes;

/// <summary>
/// Collects the "outcome" tag of every payments.processed measurement from one IMeterFactory,
/// so parallel tests with their own factories don't see each other's payments.
/// </summary>
public sealed class PaymentOutcomeRecorder : IDisposable
{
    private readonly MeterListener _listener = new();

    public PaymentOutcomeRecorder(IMeterFactory meterFactory)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Scope == meterFactory && instrument.Name == "payments.processed")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "outcome")
                {
                    Outcomes.Add((string)tag.Value!);
                }
            }
        });
        _listener.Start();
    }

    public List<string> Outcomes { get; } = new();

    public void Dispose() => _listener.Dispose();
}
