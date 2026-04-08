using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Common.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("Handling MediatR request {RequestName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            _logger.LogInformation(
                "Completed MediatR request {RequestName} in {ElapsedMilliseconds}ms",
                name,
                sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MediatR request {RequestName} failed after {ElapsedMilliseconds}ms",
                name,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}
