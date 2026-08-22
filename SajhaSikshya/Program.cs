using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using SajhaSikshya.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Hubs;
using SajhaSikshya.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging — structured (Serilog) rather than the plain console/debug providers this
// replaced. Every log line now carries machine-parseable properties (RequestId,
// UserId once enriched by ASP.NET Core's own request logging, etc.) instead of a flat
// string, and errors/warnings persist to a rolling file so a production issue can be
// diagnosed after the console/terminal that produced it is long gone. Configuration
// (minimum level, overrides) lives in appsettings*.json under "Serilog" so a deploy can
// raise verbosity without a code change.
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/sajhasikshya-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information));

// ---------------------------------------------------------------------------
// Persistence, Identity and application configuration (Extensions/ServiceCollectionExtensions.cs)
// ---------------------------------------------------------------------------
builder.Services.AddApplicationConfigurations(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationIdentity();
builder.Services.AddApplicationAuthorization();
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

// ---------------------------------------------------------------------------
// MVC, SignalR and Session
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    // Every mutating action requires a valid anti-forgery token by default;
    // Razor's <form> tag helper emits it automatically.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddSignalR();

builder.Services.AddDistributedMemoryCache();

// In-process cache for repeated-prompt AI responses (Services/AI/GeminiAIService.cs) —
// deliberately separate from the distributed cache above, which backs Session state.
builder.Services.AddMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(SecurityConstants.SessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// Rate limiting — no throttle existed anywhere before this (login brute-force is
// covered separately by Identity's own account lockout). Partitioned per signed-in
// user (falling back to remote IP for the rare anonymous case, e.g. the AI
// Assistant) so one abusive account can't exhaust the limit for everyone else.
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    string PartitionKey(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
            ? $"user:{context.User.Identity.Name}"
            : $"ip:{context.Connection.RemoteIpAddress}";

    // The AI Assistant and listing-AI features call out to the paid Gemini API —
    // tightest limit of the set, since each request has a real dollar cost.
    options.AddPolicy("ai", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context),
        _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 8, QueueLimit = 0 }));

    // Chat sends, order placement, and review submission — cheap individually but
    // still worth capping so a script can't flood a seller's inbox or spam orders/reviews.
    options.AddPolicy("write-actions", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context),
        _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 30, QueueLimit = 0 }));
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------
app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();
app.MapStaticAssets();

// MapStaticAssets() only serves files present in wwwroot at build time (it works off a
// publish-time manifest for fingerprinting/compression). User uploads (Listing photos,
// and later profile pictures, verification documents, etc.) are written to
// wwwroot/uploads/ at runtime, so they need the classic static-file middleware as well.
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");

await app.InitializeDatabaseAsync();

app.Run();
