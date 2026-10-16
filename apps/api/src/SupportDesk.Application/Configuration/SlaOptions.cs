using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Triage;

namespace SupportDesk.Application.Configuration;

public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public Dictionary<TicketPriority, double> BaseWindowHours { get; init; } = [];

    public double PremiumCustomerMultiplier { get; init; }

    public double MinimumWindowHours { get; init; }

    public double AtRiskThresholdPercent { get; init; }

    public SlaSettings ToSettings() =>
        new(
            BaseWindowHours.ToDictionary(pair => pair.Key, pair => TimeSpan.FromHours(pair.Value)),
            PremiumCustomerMultiplier,
            TimeSpan.FromHours(MinimumWindowHours),
            AtRiskThresholdPercent);
}
