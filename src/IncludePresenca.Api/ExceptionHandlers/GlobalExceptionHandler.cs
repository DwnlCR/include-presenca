using IncludePresenca.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IncludePresenca.Api.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        int statusCode = exception switch
        {
            EmailAlreadyExistsException => StatusCodes.Status409Conflict,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        ProblemDetails problem = new()
        {
            Status = statusCode,
            Title = statusCode switch
            {
                409 => "Conflict",
                401 => "Unauthorized",
                400 => "Bad Request",
                _ => "Internal Server Error"
            },
            Detail = statusCode == 500
                ? "An unexpected error occurred."
                : exception.Message
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}