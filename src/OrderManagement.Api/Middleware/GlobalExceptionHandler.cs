using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Exceptions;

namespace OrderManagement.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = exception switch
        {
            FluentValidation.ValidationException =>
                (StatusCodes.Status400BadRequest, "Validation failed", "One or more validation errors occurred."),

            NotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found", exception.Message),

            ConflictException =>
                (StatusCodes.Status409Conflict, "Conflict", exception.Message),

            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                (StatusCodes.Status409Conflict, "Conflict", "A record with the same unique value already exists."),

            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred", (string?)null)
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning("Handled {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is FluentValidation.ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}