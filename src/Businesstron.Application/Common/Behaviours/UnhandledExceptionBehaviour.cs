using MediatR;
using Microsoft.Extensions.Logging;

namespace Businesstron.Application.Common.Behaviours;

/// <summary>Logs any unhandled exception thrown by a request handler, then rethrows.</summary>
public class UnhandledExceptionBehaviour<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Businesstron request failed: {RequestName} {@Request}", typeof(TRequest).Name, request);
            throw;
        }
    }
}
