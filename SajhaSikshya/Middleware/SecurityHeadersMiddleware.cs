namespace SajhaSikshya.Middleware;

/// <summary>
/// Adds the standard defense-in-depth response headers ASP.NET Core doesn't set by
/// default. None of this replaces the app's real controls (antiforgery tokens, Identity,
/// authorization policies) — it narrows what a browser will do if something else ever
/// goes wrong (a reflected script, a rogue iframe embed, a MIME-sniffed upload).
/// Registered first in the pipeline (see Program.cs) so every response — including ones
/// <see cref="ExceptionHandlingMiddleware"/> generates — carries these headers.
/// </summary>
public class SecurityHeadersMiddleware
{
    // 'unsafe-inline' on script-src/style-src is a deliberate, documented trade-off: the
    // app's views carry many inline @section Scripts blocks and Bootstrap-driven inline
    // handlers rather than externalized/nonce'd scripts. Tightening this further is real
    // follow-up work (see the test ledger's Recommendations), not something to fake with
    // a nonce that would have to be threaded through every Razor view first.
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' ws: wss:; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
