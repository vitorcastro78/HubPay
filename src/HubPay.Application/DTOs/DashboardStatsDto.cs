namespace HubPay.Application.DTOs;

public sealed record DashboardStatsDto(
    decimal TotalVolumeEur,
    decimal ConversionRatePercent,
    int FraudsBlocked,
    int TraExemptionCount,
    int WeroA2ACount,
    int TraditionalCardCount);
