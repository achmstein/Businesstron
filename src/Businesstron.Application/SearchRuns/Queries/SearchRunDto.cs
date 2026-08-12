namespace Businesstron.Application.SearchRuns.Queries;

public class SearchRunDto
{
    public Guid Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool CancellationRequested { get; init; }
    public int TotalItems { get; init; }
    public int ProcessedItems { get; init; }
    public int FoundRecords { get; init; }
    public int SuitableCount { get; init; }
    public int PushedCount { get; init; }
    public int ErrorCount { get; init; }
    public string[] AppliedKeywords { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public string? CreatedBy { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? Error { get; init; }

    public static SearchRunDto FromEntity(SearchRun r) => new()
    {
        Id = r.Id,
        Source = r.Source.ToString(),
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        Status = r.Status.ToString(),
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
        CreatedBy = r.CreatedBy,
        Created = r.Created,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        Error = r.Error
    };
}
