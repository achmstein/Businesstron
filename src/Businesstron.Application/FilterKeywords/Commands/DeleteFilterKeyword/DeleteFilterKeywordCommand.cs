using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.FilterKeywords.Commands.DeleteFilterKeyword;

public record DeleteFilterKeywordCommand(Guid Id) : IRequest;

public class DeleteFilterKeywordCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteFilterKeywordCommand>
{
    public async Task Handle(DeleteFilterKeywordCommand request, CancellationToken cancellationToken)
    {
        var keyword = await context.FilterKeywords
            .FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FilterKeyword), request.Id);

        context.FilterKeywords.Remove(keyword);
        await context.SaveChangesAsync(cancellationToken);
    }
}
