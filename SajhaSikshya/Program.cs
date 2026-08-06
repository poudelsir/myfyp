using SajhaSikshya.Extensions;
using SajhaSikshya.Hubs;
using SajhaSikshya.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ---------------------------------------------------------------------------
// Persistence, Identity and application configuration (Extensions/ServiceCollectionExtensions.cs)
// ---------------------------------------------------------------------------
builder.Services.AddApplicationConfigurations(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationIdentity();
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
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------
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

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<NotificationHub>("/hubs/notifications");

await app.InitializeDatabaseAsync();

app.Run();
