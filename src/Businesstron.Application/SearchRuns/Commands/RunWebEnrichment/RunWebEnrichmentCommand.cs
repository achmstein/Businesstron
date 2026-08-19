using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.RunWebEnrichment;

/// <summary>
/// Starts (or re-runs) the web stage for an existing run — the "Find websites &amp;
/// contacts" / "Re-run" button. Flips the run's opt-in flag on so a later retry also
/// continues into the web stage, clears any prior stop, and enqueues the durable job.
/// No-ops when a web job is already live, so the button can't spawn a duplicate.
/// </summary>
public record RunWebEnrichmentCommand(Guid SearchRunId) : IRequest;

public class RunWebEnrichmentCommandHandler(
    IApplicationDbContext context, IJobScheduler jobs, IReverseWhoisClient reverseWhois)
    : IRequestHandler<RunWebEnrichmentCommand>
{
    public async Task Handle(RunWebEnrichmentCommand request, CancellationToken cancellationToken)
    {
        var run = await context.SearchRuns.FirstOrDefaultAsync(r => r.Id == request.SearchRunId, cancellationToken)
            ?? throw new NotFoundException(nameof(SearchRun), request.SearchRunId);

        if (!reverseWhois.IsConfigured)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(RunWebEnrichmentCommand.SearchRunId),
                    "No WhoisXML API key is configured. Set it in Settings → Reverse WHOIS before running web enrichment.")
            });
        }

        // Already live (Running with a recent heartbeat)? Do nothing — the UI shows Stop in
        // that state, so this only guards a double-click or a stale tab.
        var live = run.WebEnrichmentState == WebEnrichmentRunState.Running
            && run.WebEnrichmentHeartbeat is { } beat
            && beat > DateTimeOffset.UtcNow.AddMinutes(-5);
        if (live)
        {
            return;
        }

        run.EnableWebEnrichment = true;
        run.WebEnrichmentState = WebEnrichmentRunState.Queued;
        run.WebEnrichmentCancellationRequested = false;
        await context.SaveChangesAsync(cancellationToken);

        jobs.EnqueueWebEnrichment(run.Id);
    }
}
