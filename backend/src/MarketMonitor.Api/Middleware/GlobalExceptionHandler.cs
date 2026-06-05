using MarketMonitor.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarketMonitor.Api.Middleware;

/// <summary>
/// Centralised error handling. Expected business errors (<see cref="AppException"/>)
/// become 4xx ProblemDetails; everything else is logged and returned as a generic
/// 500 so internal details never leak to clients.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is AppException appEx)
        {
            problem = new ProblemDetails
            {
                Status = appEx.StatusCode,
                Title = appEx.StatusCode switch
                {
                    404 => "Not Found",
                    409 => "Conflict",
                    _ => "Bad Request"
                },
                Detail = appEx.Message
            };
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                // Surface details only in Development to aid debugging.
                Detail = _env.IsDevelopment() ? exception.ToString() : "Please try again later."
            };
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
