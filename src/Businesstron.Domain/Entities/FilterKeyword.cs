namespace Businesstron.Domain.Entities;

/// <summary>
/// A blacklist term. If a business name contains this word (case-insensitive)
/// the record is flagged unsuitable and excluded from the Ontraport push.
/// </summary>
public class FilterKeyword : BaseAuditableEntity
{
    public string Word { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
