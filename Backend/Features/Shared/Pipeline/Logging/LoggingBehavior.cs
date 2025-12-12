using System.Diagnostics;
using MediatR;
using Serilog;

namespace Backend.Features.Shared.Pipeline;

/// <summary>
/// MediatR pipeline behavior that logs each request with timing information
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly Serilog.ILogger _logger = Log.ForContext<LoggingBehavior<TRequest, TResponse>>();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.Information("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            _logger.Information(
                "Completed {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.Error(
                ex,
                "Request {RequestName} failed after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
