using System.Text.Json;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFileLoggerService _fileLogger;

    public ExceptionMiddleware(RequestDelegate _next, IFileLoggerService fileLogger)
    {
        this._next = _next;
        _fileLogger = fileLogger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        var traceId = context.TraceIdentifier;

        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>(validationEx.Errors))
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = validationEx.Message,
                    Instance = context.Request.Path
                };
                validationProblem.Extensions["traceId"] = traceId;
                await JsonSerializer.SerializeAsync(context.Response.Body, validationProblem);
                return;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Resource not found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = notFoundEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case ConflictException conflictEx:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Conflict detected",
                    Status = StatusCodes.Status409Conflict,
                    Detail = conflictEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case UnprocessableEntityException unprocessableEx:
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Title = "Unprocessable entity",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = unprocessableEx.Message,
                    Instance = context.Request.Path
                };
                break;

            default:
                // Log forensic entry to file for unexpected internal errors
                _fileLogger.LogError(exception, context.Request.Path, context.Request.Method, traceId);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "An unexpected error occurred while processing your request.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An internal server error occurred. Please contact support with the provided trace ID.",
                    Instance = context.Request.Path
                };
                break;
        }

        problemDetails.Extensions["traceId"] = traceId;
        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails);
    }
}
