using FamilyWall.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System;
using System.Net.Http.Headers;
using System.Net.Mime;
using FamilyWall.Database.Context;

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

app.UseStaticFiles();
app.MapRazorPages();
app.UseAuthentication();
app.UseAuthorization();

// Serve photos from the photos folder
var photosPath = Path.Combine(builder.Environment.ContentRootPath, "photos");
if (!Directory.Exists(photosPath))
{
    Directory.CreateDirectory(photosPath);
}

app.UseStaticFiles(); // Existing wwwroot

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(photosPath),
    RequestPath = "/photos"
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