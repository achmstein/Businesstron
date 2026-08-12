namespace Businesstron.Application.SearchRuns.Queries;

public class BusinessNameRecordDto
{
    public Guid Id { get; init; }
    public string? BusinessName { get; init; }
    public string? Status { get; init; }
    public string? RegistrationDate { get; init; }
    public string? RenewalDate { get; init; }
    public string? CancelledDate { get; init; }
    public string? CancellationUnderReview { get; init; }
    public string? AddressForServiceDocuments { get; init; }
    public string? PrincipalPlaceOfBusiness { get; init; }
    public string? HolderName { get; init; }
    public string? HolderType { get; init; }
    public string? HolderAbn { get; init; }
    public string? AbnStatus { get; init; }
    public string? Gst { get; init; }
    public bool HasMultipleBusinessNames { get; init; }
    public string EnrichmentStatus { get; init; } = string.Empty;
    public string? EnrichmentError { get; init; }
    public bool IsSuitable { get; init; }
    public string? SuitabilityReason { get; init; }
    public string OntraportStatus { get; init; } = string.Empty;
    public string? OntraportContactId { get; init; }
    public string? OntraportError { get; init; }
    public DateTimeOffset? PushedAt { get; init; }

    public static BusinessNameRecordDto FromEntity(BusinessNameRecord r) => new()
    {
        Id = r.Id,
        BusinessName = r.BusinessName,
        Status = r.Status,
        RegistrationDate = r.RegistrationDate,
        RenewalDate = r.RenewalDate,
        CancelledDate = r.CancelledDate,
        CancellationUnderReview = r.CancellationUnderReview,
        AddressForServiceDocuments = r.AddressForServiceDocuments,
        PrincipalPlaceOfBusiness = r.PrincipalPlaceOfBusiness,
        HolderName = r.HolderName,
        HolderType = r.HolderType,
        HolderAbn = r.HolderAbn,
        AbnStatus = r.AbnStatus,
        Gst = r.Gst,
        HasMultipleBusinessNames = r.HasMultipleBusinessNames,
        EnrichmentStatus = r.EnrichmentStatus.ToString(),
        EnrichmentError = r.EnrichmentError,
        IsSuitable = r.IsSuitable,
        SuitabilityReason = r.SuitabilityReason,
        OntraportStatus = r.OntraportStatus.ToString(),
        OntraportContactId = r.OntraportContactId,
        OntraportError = r.OntraportError,
        PushedAt = r.PushedAt
    };
}
