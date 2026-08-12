using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.Settings.Commands.UpdateOntraportConfiguration;

public record UpdateOntraportConfigurationCommand(int? TagId, int? SequenceId, bool AutoPushEnabled) : IRequest;

public class UpdateOntraportConfigurationCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateOntraportConfigurationCommand>
{
    public async Task Handle(UpdateOntraportConfigurationCommand request, CancellationToken cancellationToken)
    {
        var config = await context.OntraportConfigurations.FirstOrDefaultAsync(cancellationToken);

        if (config is null)
        {
            config = new OntraportConfiguration();
            context.OntraportConfigurations.Add(config);
        }

        config.TagId = request.TagId;
        config.SequenceId = request.SequenceId;
        config.AutoPushEnabled = request.AutoPushEnabled;

        await context.SaveChangesAsync(cancellationToken);
    }
}
