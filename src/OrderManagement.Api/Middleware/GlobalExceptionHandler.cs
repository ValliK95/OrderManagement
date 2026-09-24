using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Exceptions;
using FluentValidation;

namespace OrderManagement.Api.Middleware
{
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
            var (status, title) = exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception");

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status switch
                {
                    StatusCodes.Status500InternalServerError => null, 
                    StatusCodes.Status400BadRequest => "One or more validation errors occurred.",
                    _ => exception.Message
                }
            };

            if (exception is ValidationException validationException)
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
}
