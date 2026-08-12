namespace Businesstron.Application.Common.Interfaces;

public sealed record AsicHolder(string? Name, string? Type, string? Abn);

public sealed record AsicBusinessName(
    string? Name,
    string? Status,
    string? RegistrationDate,
    string? RenewalDate,
    string? CancelledDate,
    string? CancellationUnderReview,
    string? AddressForServiceDocuments,
    string? PrincipalPlaceOfBusiness,
    IReadOnlyList<AsicHolder> Holders);

public sealed record AsicSearchResult(bool Succeeded, IReadOnlyList<AsicBusinessName> BusinessNames, bool HasMultiple = false, string? Reason = null)
{
    public static AsicSearchResult Fail(string? reason = null) => new(false, [], false, reason);
    public static AsicSearchResult Ok(IReadOnlyList<AsicBusinessName> names, bool hasMultiple = false) => new(true, names, hasMultiple);
}

/// <summary>
/// Queries the ASIC business-names registry (connectonline.asic.gov.au), solving
/// the reCAPTCHA gate as needed. Wraps the fragile ADF/JSF navigation flow.
/// </summary>
public interface IAsicRegistryClient
{
    /// <summary>Return every business name held under an ABN.</summary>
    Task<AsicSearchResult> SearchByAbnAsync(string abn, CancellationToken cancellationToken);

    /// <summary>Return the single business name matching a name, when the ABN maps to several.</summary>
    Task<AsicSearchResult> SearchByAbnAndNameAsync(string abn, string businessName, CancellationToken cancellationToken);
}
