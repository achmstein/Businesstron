using Businesstron.Domain.Services;
using NUnit.Framework;
using Shouldly;

namespace Businesstron.Domain.UnitTests.Services;

public class RenewalWindowTests
{
    // Fixed "today" so the window maths is deterministic.
    private static readonly DateOnly Today = new(2026, 8, 12);

    [Test]
    public void Renewal_within_12_months_is_in_window()
    {
        // 15/02/2027 is ~6 months out — within the 12-month lead window.
        RenewalWindow.IsWithinMonths("15/02/2027", Today).ShouldBeTrue();
    }

    [Test]
    public void Renewal_beyond_12_months_is_out_of_window()
    {
        // 01/03/2028 is well past 12 months.
        RenewalWindow.IsWithinMonths("01/03/2028", Today).ShouldBeFalse();
    }

    [Test]
    public void Overdue_renewal_counts_as_in_window()
    {
        // Already past due — the hottest lead of all.
        RenewalWindow.IsWithinMonths("01/01/2025", Today).ShouldBeTrue();
    }

    [Test]
    public void Exactly_12_months_is_in_window()
    {
        RenewalWindow.IsWithinMonths("12/08/2027", Today).ShouldBeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not a date")]
    public void Blank_or_unparseable_is_out_of_window(string? value)
    {
        RenewalWindow.IsWithinMonths(value, Today).ShouldBeFalse();
    }

    [TestCase("15/02/2027")]   // dd/MM/yyyy (ASIC's format)
    [TestCase("2027-02-15")]   // ISO
    [TestCase("15 Feb 2027")]  // day month-name year
    public void Parses_common_date_formats(string value)
    {
        RenewalWindow.TryParse(value, out var date).ShouldBeTrue();
        date.ShouldBe(new DateOnly(2027, 2, 15));
    }
}
