using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
            
        _logger.LogInformation("Handling request {RequestName} {@Request}", 
            requestName, request);

        try
        {
            var response = await next();
                
            _logger.LogInformation("Request {RequestName} handled successfully", 
                requestName);
                
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request {RequestName}", requestName);
            throw;
        }
    }
}