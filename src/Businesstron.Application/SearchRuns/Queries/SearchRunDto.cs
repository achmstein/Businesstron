using Businesstron.Domain.Enums;

namespace Businesstron.Application.SearchRuns.Queries;

public class SearchRunDto
{
    public Guid Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    /// <summary>The raw ASIC-stage status (Pending/Running/Completed/Failed/Cancelled).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Stage-aware status for the badge: reflects the whole pipeline, so a run whose ASIC
    /// stage is done but whose web stage is still going reads "Enriching web", not "Completed".
    /// </summary>
    public string OverallStatus { get; init; } = string.Empty;

    /// <summary>Run-level web-enrichment state (for list-view badges/indicators).</summary>
    public string WebEnrichmentState { get; init; } = nameof(WebEnrichmentRunState.NotRequested);
    public bool CancellationRequested { get; init; }
    public int TotalItems { get; init; }
    public int ProcessedItems { get; init; }
    public int FoundRecords { get; init; }
    public int SuitableCount { get; init; }
    public int PushedCount { get; init; }
    public int ErrorCount { get; init; }
    public string[] AppliedKeywords { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public bool EnableWebEnrichment { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? Error { get; init; }

    /// <summary>Records with a contact email (list view only; null when not aggregated).</summary>
    public int? WebEmailCount { get; init; }

    /// <summary>Records still queued for the web stage — &gt; 0 means it is running (list view only).</summary>
    public int? WebPendingCount { get; init; }

    public static SearchRunDto FromEntity(SearchRun r, int? webEmailCount = null, int? webPendingCount = null) => new()
    {
        Id = r.Id,
        Source = r.Source.ToString(),
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        Status = r.Status.ToString(),
        OverallStatus = DeriveOverallStatus(r),
        WebEnrichmentState = r.WebEnrichmentState.ToString(),
        CancellationRequested = r.CancellationRequested,
        TotalItems = r.TotalItems,
        ProcessedItems = r.ProcessedItems,
        FoundRecords = r.FoundRecords,
        SuitableCount = r.SuitableCount,
        PushedCount = r.PushedCount,
        ErrorCount = r.ErrorCount,
        AppliedKeywords = string.IsNullOrWhiteSpace(r.AppliedKeywords)
            ? []
            : r.AppliedKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        Tags = string.IsNullOrWhiteSpace(r.Tags)
            ? []
            : r.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        EnableWebEnrichment = r.EnableWebEnrichment,
        CreatedBy = r.CreatedBy,
        Created = r.Created,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        Error = r.Error,
        WebEmailCount = webEmailCount,
        WebPendingCount = webPendingCount
    };

    /// <summary>
    /// Rolls the ASIC status and the web-stage state into one badge value. The run only
    /// reads "Completed" once every enabled stage is done; while the web stage runs it
    /// reports that stage instead.
    /// </summary>
    private static string DeriveOverallStatus(SearchRun r)
    {
        // Any ASIC-stage state other than Completed is the overall state — the web stage
        // hasn't started yet.
        if (r.Status != SearchRunStatus.Completed)
        {
            return r.Status.ToString();
        }

        if (!r.EnableWebEnrichment)
        {
            return nameof(SearchRunStatus.Completed);
        }

        return r.WebEnrichmentState switch
        {
            WebEnrichmentRunState.Queued => "WebQueued",
            WebEnrichmentRunState.Running => "EnrichingWeb",
            WebEnrichmentRunState.Cancelled => "WebStopped",
            WebEnrichmentRunState.Failed => "WebFailed",
            // Completed, or NotRequested (opted in but never handed off yet).
            _ => nameof(SearchRunStatus.Completed)
        };
    }
}
