using FamilyWall.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System;
using System.Net.Http.Headers;
using System.Net.Mime;
using FamilyWall.Database.Context;
using FamilyWall.Database.Interfaces;
using Microsoft.Net.Http.Headers;
using CacheControlHeaderValue = Microsoft.Net.Http.Headers.CacheControlHeaderValue;

/*
Plan / Pseudocode (detailed):

1. Goal: Ensure the app returns responses that disable caching everywhere.
2. Apply a global middleware that runs for every request and sets:
   - Cache-Control: no-cache, no-store, must-revalidate
   - Pragma: no-cache
   - Expires: -1
   This will cover dynamic responses (Razor pages, API, etc).

3. Static files may be served directly by the Static File Middleware and may set their own headers.
   To guarantee static files also have no-cache headers, replace the plain `UseStaticFiles()` call
   with `UseStaticFiles(new StaticFileOptions { OnPrepareResponse = ... })` that sets the same headers
   inside `OnPrepareResponse`.

4. The photos static file provider already uses `StaticFileOptions`; augment it to set headers as well.

5. Middleware ordering:
   - Register the global no-cache middleware before static files so it can run for all requests.
   - Also keep per-static-file OnPrepareResponse to ensure files served by the static file middleware
     have headers set even if other middleware behaves differently.

6. Keep existing behavior (authentication, authorization, launching browser, etc.) unchanged.

Implementation steps:
 - Add `using Microsoft.Net.Http.Headers;` for header helpers.
 - Insert a global middleware `app.Use(...)` that sets typed headers and raw headers.
 - Replace the two `UseStaticFiles()` calls to include `OnPrepareResponse` that sets no-cache headers.
 - Ensure the rest of the pipeline runs as before.
*/

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(config =>
{
    config.AddFilter("Microsoft", LogLevel.Warning);
    config.AddFilter("System", LogLevel.Warning);
});

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(["Files.Read", "Tasks.ReadWrite"])
    .AddInMemoryTokenCaches();

builder.Services.AddMemoryCache();
builder.Services.AddMicrosoftIdentityConsentHandler();
builder.Services.AddAuthorization();
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

string userAgent = builder.Configuration["WallSettings:UserAgent"] ?? "FamilyWall/1.0";

builder.Services.AddSingleton<GoogleCalendarService>();
builder.Services.AddSingleton<OneDriveImageService>();
builder.Services.AddSingleton<NwsWeatherClient>();

builder.Services.AddHttpClient("nws", c =>
{
    c.BaseAddress = new Uri("https://api.weather.gov/");
    c.Timeout = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/geo+json"));
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
});

builder.Services.AddHttpClient("microsoft-graph", c =>
{
    c.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
    c.Timeout = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
});

builder.Services.AddSingleton<IFamilyWallDataContext>(new FamilyWallDbContext("Filename=family-wall.db;Connection=shared"));

// Configure Kestrel for local access
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(8888);
});

var app = builder.Build();

// Global middleware to disable caching on all responses
app.Use(async (context, next) =>
{
    // Set typed Cache-Control header
    context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
    {
        NoCache = true,
        NoStore = true,
        MustRevalidate = true
    };

    // Set legacy headers for broader compatibility
    context.Response.Headers[HeaderNames.Pragma] = "no-cache";
    context.Response.Headers[HeaderNames.Expires] = "-1";

    await next();
});

// Serve static files (wwwroot) with explicit no-cache headers for prepared responses
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true
        };
        ctx.Context.Response.Headers[HeaderNames.Pragma] = "no-cache";
        ctx.Context.Response.Headers[HeaderNames.Expires] = "-1";
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Serve photos from the photos folder
var photosPath = Path.Combine(builder.Environment.ContentRootPath, "photos");
if (!Directory.Exists(photosPath))
{
    Directory.CreateDirectory(photosPath);
}

// Serve photos with no-cache headers
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(photosPath),
    RequestPath = "/photos",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true
        };
        ctx.Context.Response.Headers[HeaderNames.Pragma] = "no-cache";
        ctx.Context.Response.Headers[HeaderNames.Expires] = "-1";
    }
});

if (builder.Configuration["WallSettings:KioskMode"]?.ToLower() == "true")
{
    // Launch browser after a short delay
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000); // Give server time to start
        LaunchKiosk("http://localhost:8888");
    });
}
else
{
    // Launch browser after a short delay
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000); // Give server time to start
        LaunchBrowser("http://localhost:8888");
    });
}

app.Run();

static void LaunchKiosk(string url)
{
    try
    {
        // Raspberry Pi OS typically uses Chromium
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "chromium-browser",
            Arguments = $"--kiosk --noerrdialogs --disable-infobars {url}",
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to launch browser: {ex.Message}");
    }
}

static void LaunchBrowser(string url)
{
    try
    {
        // Raspberry Pi OS typically uses Chromium
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "chromium-browser",
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to launch browser: {ex.Message}");
    }
}