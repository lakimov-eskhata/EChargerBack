using Application.Common.Exceptions;
using Application.Common.Response;

namespace Admin.API.Middleware;

public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred");
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain error occurred");
                await HandleDomainExceptionAsync(context, ex);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found");
                await HandleNotFoundExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var response = new ApiErrorResponse(
                "Validation failed",
                exception.Errors.Select(e => e.Key));

            return context.Response.WriteAsJsonAsync(response);
        }

        private static Task HandleDomainExceptionAsync(HttpContext context, DomainException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status409Conflict;

            var response = new ApiErrorResponse(exception.Message);

            return context.Response.WriteAsJsonAsync(response);
        }

        private static Task HandleNotFoundExceptionAsync(HttpContext context, NotFoundException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            var response = new ApiErrorResponse(exception.Message);

            return context.Response.WriteAsJsonAsync(response);
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = new ApiErrorResponse("An internal server error occurred");

            return context.Response.WriteAsJsonAsync(response);
        }
    }