namespace Businesstron.Domain.Entities;

/// <summary>
/// Single-row, UI-editable Ontraport push configuration. API credentials live in
/// app configuration/environment; this row holds the operational mapping (which
/// tag/sequence suitable leads are added to, and whether pushing is automatic).
/// </summary>
public class OntraportConfiguration : BaseAuditableEntity
{
    /// <summary>Ontraport tag id to apply to each pushed contact (optional).</summary>
    public int? TagId { get; set; }

    /// <summary>Ontraport sequence/campaign id to subscribe each pushed contact to (optional).</summary>
    public int? SequenceId { get; set; }

    /// <summary>When true, suitable records are pushed automatically at the end of a search run.</summary>
    public bool AutoPushEnabled { get; set; }
}
