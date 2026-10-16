using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

public sealed class SlaSettings
{
    private readonly IReadOnlyDictionary<TicketPriority, TimeSpan> _baseWindows;

    public SlaSettings(
        IReadOnlyDictionary<TicketPriority, TimeSpan> baseWindows,
        double premiumMultiplier,
        TimeSpan minimumWindow,
        double atRiskThresholdPercent)
    {
        ArgumentNullException.ThrowIfNull(baseWindows);

        foreach (var priority in Enum.GetValues<TicketPriority>())
        {
            if (!baseWindows.TryGetValue(priority, out var window) || window <= TimeSpan.Zero)
            {
                throw new ArgumentException($"A positive SLA window is required for {priority} priority.", nameof(baseWindows));
            }
        }

        if (premiumMultiplier is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(premiumMultiplier), premiumMultiplier, "Must be greater than 0 and at most 1.");
        }

        if (minimumWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumWindow), minimumWindow, "Must be positive.");
        }

        if (atRiskThresholdPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(atRiskThresholdPercent), atRiskThresholdPercent, "Must be between 0 and 100.");
        }

        _baseWindows = new Dictionary<TicketPriority, TimeSpan>(baseWindows);
        PremiumMultiplier = premiumMultiplier;
        MinimumWindow = minimumWindow;
        AtRiskThresholdPercent = atRiskThresholdPercent;
    }

    public double PremiumMultiplier { get; }

    public TimeSpan MinimumWindow { get; }

    public double AtRiskThresholdPercent { get; }

    public TimeSpan BaseWindowFor(TicketPriority priority) => _baseWindows[priority];
}
