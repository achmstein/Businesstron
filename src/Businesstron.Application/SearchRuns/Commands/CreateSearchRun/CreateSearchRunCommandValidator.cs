using FluentValidation;

namespace Businesstron.Application.SearchRuns.Commands.CreateSearchRun;

public class CreateSearchRunCommandValidator : AbstractValidator<CreateSearchRunCommand>
{
    public CreateSearchRunCommandValidator()
    {
        When(x => x.Source == SearchSource.DataGov, () =>
        {
            RuleFor(x => x.StartDate)
                .NotNull().WithMessage("Start date is required for a data.gov.au search.")
                .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Start date cannot be in the future.");

            RuleFor(x => x.EndDate)
                .NotNull().WithMessage("End date is required for a data.gov.au search.");

            RuleFor(x => x)
                .Must(x => x.StartDate is null || x.EndDate is null || x.StartDate <= x.EndDate)
                .WithMessage("Start date cannot be after end date.")
                .WithName(nameof(CreateSearchRunCommand.EndDate));
        });

        When(x => x.Source == SearchSource.AbnList, () =>
        {
            RuleFor(x => x.AbnListRaw)
                .NotEmpty().WithMessage("Provide at least one ABN.")
                .Must(HasAtLeastOneAbn).WithMessage("Provide at least one non-empty ABN line.");
        });
    }

    private static bool HasAtLeastOneAbn(string? raw) =>
        !string.IsNullOrWhiteSpace(raw) &&
        raw.Split(['\n', '\r', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 0;
}
