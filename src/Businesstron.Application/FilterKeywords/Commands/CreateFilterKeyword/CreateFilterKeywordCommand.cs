using Businesstron.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.FilterKeywords.Commands.CreateFilterKeyword;

public record CreateFilterKeywordCommand(string Word) : IRequest<Guid>;

public class CreateFilterKeywordCommandValidator : AbstractValidator<CreateFilterKeywordCommand>
{
    public CreateFilterKeywordCommandValidator()
    {
        RuleFor(x => x.Word).NotEmpty().MaximumLength(100);
    }
}

public class CreateFilterKeywordCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateFilterKeywordCommand, Guid>
{
    public async Task<Guid> Handle(CreateFilterKeywordCommand request, CancellationToken cancellationToken)
    {
        var word = request.Word.Trim();

        var existing = await context.FilterKeywords
            .FirstOrDefaultAsync(k => k.Word.ToLower() == word.ToLower(), cancellationToken);

        if (existing is not null)
        {
            existing.IsActive = true;
            await context.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var keyword = new FilterKeyword { Word = word, IsActive = true };
        context.FilterKeywords.Add(keyword);
        await context.SaveChangesAsync(cancellationToken);
        return keyword.Id;
    }
}
