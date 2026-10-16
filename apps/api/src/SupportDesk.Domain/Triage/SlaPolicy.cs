using System.Globalization;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

public sealed class SlaPolicy(SlaSettings settings)
{
    public SlaWindow WindowFor(TicketPriority priority, CustomerTier tier)
    {
        var baseWindow = settings.BaseWindowFor(priority);
        var baseReason = $"The {priority} window is {Hours(baseWindow)}";

        if (tier != CustomerTier.Premium)
        {
            return new SlaWindow(baseWindow, $"{baseReason}.");
        }

        var reduced = baseWindow * settings.PremiumMultiplier;

        if (reduced < settings.MinimumWindow)
        {
            return new SlaWindow(
                settings.MinimumWindow,
                $"{baseReason}; the premium reduction to {Hours(reduced)} is raised to the {Hours(settings.MinimumWindow)} minimum.");
        }

        return new SlaWindow(reduced, $"{baseReason}; premium customers get {Hours(reduced)}.");
    }

    private static string Hours(TimeSpan window) =>
        $"{window.TotalHours.ToString("0.##", CultureInfo.InvariantCulture)} h";
}
