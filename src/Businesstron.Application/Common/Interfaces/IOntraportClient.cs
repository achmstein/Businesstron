namespace Businesstron.Application.Common.Interfaces;

/// <summary>Fields we push to Ontraport for a business-name lead.</summary>
public sealed record OntraportContactInput(
    string? BusinessName,
    string? HolderName,
    string? Abn,
    string? Address,
    string? Email);

public sealed record OntraportContactResult(bool Succeeded, string? ContactId, string? Error);

/// <summary>Ontraport REST client. Implemented in Infrastructure over their HTTP API.</summary>
public interface IOntraportClient
{
    /// <summary>True when API credentials are present in configuration.</summary>
    bool IsConfigured { get; }

    /// <summary>Create or merge a contact from the record fields.</summary>
    Task<OntraportContactResult> UpsertContactAsync(OntraportContactInput input, CancellationToken cancellationToken);

    /// <summary>Apply a tag to a contact (no-op when tagId is null).</summary>
    Task AddTagAsync(string contactId, int? tagId, CancellationToken cancellationToken);

    /// <summary>Subscribe a contact to a sequence/campaign (no-op when sequenceId is null).</summary>
    Task AddToSequenceAsync(string contactId, int? sequenceId, CancellationToken cancellationToken);
}
