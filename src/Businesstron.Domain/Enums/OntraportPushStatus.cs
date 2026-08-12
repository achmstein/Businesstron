namespace Businesstron.Domain.Enums;

public enum OntraportPushStatus
{
    NotPushed = 0,
    Pushed = 1,
    Failed = 2,
    /// <summary>Excluded from Ontraport because the record was flagged unsuitable by the keyword filter.</summary>
    Skipped = 3
}
