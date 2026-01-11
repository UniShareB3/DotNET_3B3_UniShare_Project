using System.Diagnostics;
using MediatR;
using Serilog;

namespace Backend.Features.Shared.Pipeline.Logging;

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
        var success = false; // Track execution status

        try
        {
            _logger.Information("Handling {RequestName}", requestName);

            var response = await next(cancellationToken);
        
            // If we reach this line, no exception was thrown
            success = true; 
            return response;
        }
        finally
        {
            stopwatch.Stop();

            if (success)
            {
                _logger.Information(
                    "Completed {RequestName} in {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                // We know it failed because success is false.
                // We log the PERFORMANCE metric here, but we let the 
                // Global Exception Handler log the actual Exception stack trace.
                _logger.Error(
                    "Request {RequestName} failed after {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
