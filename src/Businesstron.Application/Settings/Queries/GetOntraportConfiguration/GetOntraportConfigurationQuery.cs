using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.Settings.Queries.GetOntraportConfiguration;

public record OntraportConfigurationDto(int? TagId, int? SequenceId, bool AutoPushEnabled, bool IsConfigured);

public record GetOntraportConfigurationQuery : IRequest<OntraportConfigurationDto>;

public class GetOntraportConfigurationQueryHandler(IApplicationDbContext context, IOntraportClient ontraport)
    : IRequestHandler<GetOntraportConfigurationQuery, OntraportConfigurationDto>
{
    public async Task<OntraportConfigurationDto> Handle(GetOntraportConfigurationQuery request, CancellationToken cancellationToken)
    {
        var config = await context.OntraportConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return new OntraportConfigurationDto(
            config?.TagId,
            config?.SequenceId,
            config?.AutoPushEnabled ?? false,
            ontraport.IsConfigured);
    }
}
