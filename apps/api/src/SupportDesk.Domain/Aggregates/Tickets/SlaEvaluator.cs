namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(
        DateTime? slaStartedAtUtc,
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        double atRiskThresholdPercent)
    {
        if (dueAtUtc is not { } due)
        {
            return SlaStatus.NotApplicable;
        }

        if (resolvedAtUtc is { } resolved)
        {
            return resolved <= due ? SlaStatus.Met : SlaStatus.Breached;
        }

        if (nowUtc > due)
        {
            return SlaStatus.Breached;
        }

        return IsAtRisk(slaStartedAtUtc, due, nowUtc, atRiskThresholdPercent)
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }

    private static bool IsAtRisk(DateTime? startedAtUtc, DateTime dueAtUtc, DateTime nowUtc, double thresholdPercent)
    {
        if (startedAtUtc is not { } started || dueAtUtc <= started)
        {
            return false;
        }

        var remaining = (dueAtUtc - nowUtc).TotalSeconds;
        var window = (dueAtUtc - started).TotalSeconds;

        return remaining * 100 <= thresholdPercent * window;
    }
}
