using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.RunWebEnrichment;

/// <summary>
/// Kicks off (or re-runs) the web stage for an existing run — the "Find websites &amp;
/// contacts" button. Flips the run's opt-in flag on so a later retry also continues into
/// the web stage, then enqueues the durable job.
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

        run.EnableWebEnrichment = true;
        await context.SaveChangesAsync(cancellationToken);

        jobs.EnqueueWebEnrichment(run.Id);
    }
}
