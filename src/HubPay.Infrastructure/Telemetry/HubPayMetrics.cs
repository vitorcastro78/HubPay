using System.Diagnostics.Metrics;

namespace HubPay.Infrastructure.Telemetry;

public static class HubPayMetrics
{
    public const string MeterName = "HubPay";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> PaymentsCreated = Meter.CreateCounter<long>("hubpay.payments.created");
    public static readonly Histogram<double> AntiFraudLatencyMs = Meter.CreateHistogram<double>("hubpay.antifraud.latency_ms");
}
