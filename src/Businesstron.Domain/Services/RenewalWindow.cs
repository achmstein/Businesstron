using System.Globalization;

namespace Businesstron.Domain.Services;

/// <summary>
/// Pure rule deciding whether a business name's renewal falls inside the lead
/// window. Mirrors the client's manual step: only chase businesses whose name is
/// due to renew soon (within 12 months). ASIC renders the renewal date as an
/// Australian <c>dd/MM/yyyy</c> string, so this parses defensively and treats an
/// unparseable/blank date as out of window.
/// </summary>
public static class RenewalWindow
{
    /// <summary>Default lead window: renew within the next 12 months.</summary>
    public const int DefaultMonths = 12;

    private static readonly string[] Formats =
    {
        "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy",
        "dd-MM-yyyy", "yyyy-MM-dd", "dd MMM yyyy", "d MMM yyyy", "dd MMMM yyyy"
    };

    /// <summary>
    /// True when <paramref name="renewalDate"/> parses to a date on or before
    /// <paramref name="today"/> + <paramref name="months"/>. Past-due dates count as
    /// in window (they are the most overdue, hottest leads); blank or unparseable
    /// dates do not.
    /// </summary>
    public static bool IsWithinMonths(string? renewalDate, DateOnly today, int months = DefaultMonths)
    {
        if (!TryParse(renewalDate, out var date))
        {
            return false;
        }

        return date <= today.AddMonths(months);
    }

    /// <summary>Parses ASIC's renewal-date string, trying AU formats before invariant/current culture.</summary>
    public static bool TryParse(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        if (DateOnly.TryParseExact(trimmed, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        // Fall back to AU culture (dd/MM/yyyy) then invariant for anything unusual.
        var au = CultureInfo.GetCultureInfo("en-AU");
        return DateOnly.TryParse(trimmed, au, DateTimeStyles.None, out date)
            || DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
