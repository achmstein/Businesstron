namespace Businesstron.Application.Common.Interfaces;

/// <summary>ABN details from the Australian Business Register (abr.business.gov.au).</summary>
public sealed record AbrDetails(string? AbnStatus, string? Gst);

/// <summary>Looks up ABN status and GST registration from the ABR.</summary>
public interface IAbrClient
{
    Task<AbrDetails> GetAbnDetailsAsync(string abn, CancellationToken cancellationToken);
}
