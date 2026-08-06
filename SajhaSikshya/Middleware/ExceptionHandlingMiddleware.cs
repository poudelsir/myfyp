using System.Net;
using System.Text.Json;

namespace SajhaSikshya.Middleware;

/// <summary>
/// Catches unhandled exceptions that escape controller action filters, logs them with
/// full context, and returns a safe, generic response instead of leaking stack traces.
/// Registered early in the pipeline (see Program.cs) so it wraps everything downstream.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(context);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context)
    {
        // API/AJAX requests get a JSON envelope; browser navigations get redirected
        // to the friendly error page rendered by HomeController.Error.
        var wantsJson = context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true)
            || context.Request.Headers.XRequestedWith == "XMLHttpRequest";

        if (wantsJson)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred. Please try again later.",
                traceId = context.TraceIdentifier,
            });
            return context.Response.WriteAsync(payload);
        }

        context.Response.Redirect("/Home/Error");
        return Task.CompletedTask;
    }
}
