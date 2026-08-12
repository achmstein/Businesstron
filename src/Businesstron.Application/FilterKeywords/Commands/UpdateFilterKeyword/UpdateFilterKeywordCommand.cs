using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.FilterKeywords.Commands.UpdateFilterKeyword;

public record UpdateFilterKeywordCommand(Guid Id, string Word, bool IsActive) : IRequest;

public class UpdateFilterKeywordCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateFilterKeywordCommand>
{
    public async Task Handle(UpdateFilterKeywordCommand request, CancellationToken cancellationToken)
    {
        var keyword = await context.FilterKeywords
            .FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FilterKeyword), request.Id);

        keyword.Word = request.Word.Trim();
        keyword.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);
    }
}
