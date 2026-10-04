using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Services;
using PaymentGateway.Api.Tests.Fakes;

namespace PaymentGateway.Api.Tests;

public class PaymentMetricsTest
{
    [Fact]
    public void ShouldCountPaymentsByOutcome()
    {
        IMeterFactory meterFactory = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();
        using PaymentOutcomeRecorder recorder = new(meterFactory);
        PaymentMetrics metrics = new(meterFactory);

        metrics.RecordPayment("Authorized");
        metrics.RecordPayment("Declined");
        metrics.RecordPayment("Declined");

        recorder.Outcomes.ShouldBe(new[] { "Authorized", "Declined", "Declined" });
    }
}
