using System.ComponentModel.DataAnnotations;
using FamilyWall.Models;
using FamilyWall.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace FamilyWall.Pages;

[Authorize]
[AuthorizeForScopes(Scopes = new[] { "Files.Read", "Tasks.ReadWrite" })]
public class IndexModel(
    IMemoryCache cache,
    IWebHostEnvironment env,
    GoogleCalendarService calendarService, 
    NwsWeatherClient client, 
    OneDriveRandomImageService oneDriveRandomImageService,
    IHttpClientFactory httpClientFactory,
    ITokenAcquisition tokenAcquisition,
    IConfiguration configuration,
    MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
    ILogger<IndexModel> logger) : PageModel
{
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly Random _random = Random.Shared;

    [BindProperty]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public required DateTimeOffset StartDate { get; set; } = RoundUpToNearest(DateTimeOffset.Now, TimeSpan.FromMinutes(30));

    [BindProperty]
    [Display(Name = "End Date")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public required DateTimeOffset EndDate { get; set; } = RoundUpToNearest(DateTimeOffset.Now.AddHours(1), TimeSpan.FromMinutes(30));

    [BindProperty]
    public required string Title { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public string? Location { get; set; }

    [BindProperty]
    public required bool AllDay { get; set; }

    public async Task<IActionResult> OnGetCalendarDelete(string id)
    {
        await calendarService.DeleteEventAsync(id);

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostCalendarAdd()
    {
        await calendarService.CreateEventAsync(new FullCalendarEventItem()
        {
            Title = Title,
            Color = "0",
            Description = Description,
            EndDateTime = EndDate.DateTime,
            IsAllDay = AllDay,
            Location = Location,
            StartDateTime = StartDate.DateTime
        });

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnGet()
    {
        try
        {
            await tokenAcquisition.GetAccessTokenForUserAsync(["Files.Read", "Tasks.ReadWrite"]);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            logger.LogWarning($"Sending Challenge");
            consentHandler.HandleException(ex);
            return Challenge();
        }

        return Page();
    }

    public async Task<IActionResult> OnGetCurrentForecastAsync()
    {
        try
        {
            NationalWeatherServiceForecastResponse? forecast = await client.GetGridpointForecastAsync(CancellationToken.None);
            NationalWeatherServiceObservation? observation = await client.GetStationObservationAsync(CancellationToken.None);

            forecast ??= new NationalWeatherServiceForecastResponse();
            forecast.Observation = observation;

            return new JsonResult(forecast);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading current forecast");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnGetCurrentObservationAsync()
    {
        try
        {
            var observation = await client.GetStationObservationAsync(CancellationToken.None);
            return new JsonResult(observation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading current observation");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public Dictionary<string, int> Photos { get; set; } = new();

    // PSEUDOCODE / PLAN:
    // 1. Build path to "photos" folder.
    // 2. If folder exists:
    //    a. Enumerate files with supported extensions and convert to "/photos/{filename}" URLs.
    //    b. Ensure the `Photos` dictionary is initialized:
    //       - If null or empty, create entries for each photo with count 0.
    //       - If already present, add any new photos with count 0 and remove entries for missing files.
    //    c. Find the minimum pick count among tracked photos.
    //    d. Build a list of candidate photos that have that minimum count.
    //    e. Pick a random photo from the candidates.
    //    f. Increment the pick count for the selected photo.
    //    g. Return the selected photo URL in a JSON response.
    // 3. If folder doesn't exist, return OkResult().
    public IActionResult OnGetRandomImage()
    {
        var photosFolder = Path.Combine(env.ContentRootPath, "photos");

        if (Directory.Exists(photosFolder))
        {
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            var photos = Directory.GetFiles(photosFolder)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => $"/photos/{Path.GetFileName(f)}")
                .ToList();

            // Initialize or reconcile the Photos dictionary
            if (cache.TryGetValue("Photos", out Dictionary<string, int>? cachedPhotos) && cachedPhotos != null)
            {
                Photos = cachedPhotos;
            }
            else
            {
                // Add new photos with count 0
                foreach (var p in photos)
                {
                    Photos.TryAdd(p, 0);
                }

                // Remove entries for files that no longer exist
                var keysToRemove = Photos.Keys.Except(photos).ToList();
                foreach (var k in keysToRemove)
                {
                    Photos.Remove(k);
                }
            }

            // Determine least-picked photos and pick randomly among them
            var minCount = Photos.Values.DefaultIfEmpty(0).Min();
            var candidates = Photos.Where(kvp => kvp.Value == minCount).Select(kvp => kvp.Key).ToList();

            var photo = candidates.Count == 1 ? candidates[0] : candidates[_random.Next(candidates.Count)];

            // Increment pick count
            Photos.TryGetValue(photo, out var currentCount);
            Photos[photo] = currentCount + 1;

            cache.Set("Photos", Photos, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(2)
            });

            var path = Path.Combine(photosFolder, $"{Path.GetFileNameWithoutExtension(photo)}.json");
            if (System.IO.File.Exists(path))
            {
                var readAllText = System.IO.File.ReadAllText(path);
                var details =
                    JsonSerializer.Deserialize<OneDriveItem>(
                        readAllText, _json);

                return new JsonResult(new
                {
                    url = photo, 
                    taken = details?.Photo?.TakenDateTime?.ToString("f"),
                    alltitude = details?.Location?.Altitude,
                    longitude = details?.Location?.Longitude,
                    latitude = details?.Location?.Latitude,
                    camera = details?.Photo?.CameraMake,
                    cameraModel = details?.Photo?.CameraModel
                });
            }

            return new JsonResult(new { url = photo });
        }

        return new OkResult();
    }

    public async Task<IActionResult> OnGetToDoAsync()
    {
        string id = configuration["WallSettings:Microsoft:ToDoListId"] ?? throw new NullReferenceException("The 'WallSettings:Microsoft:ToDoListId' is null"); 
        var hc = httpClientFactory.CreateClient();
        hc.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
        var ct = CancellationToken.None;

        string accessToken;

        try
        {
            accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(["Files.Read", "Tasks.ReadWrite"]);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            logger.LogWarning($"Sending Challenge");
            consentHandler.HandleException(ex);
            return Challenge();
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, $"me/todo/lists/{id}/tasks?$filter=status eq 'notStarted'");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var resp = await hc.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var page = await JsonSerializer.DeserializeAsync<ToDoResponse>(stream, _json, ct)
                   ?? new ToDoResponse();

        var toDoItemModels = page.Value.Select(x => new ToDoItemModel(x)).ToList();

        // Load icon mappings from configuration: "ToDoIconMappings"
        // Each mapping has an "Icon" and "Keywords" array.
        // For each todo item:
        //   - find first mapping where any keyword appears in the item title (case-insensitive)
        //   - set item.Icon to mapping.Icon if found
        //   - otherwise set item.Icon to default "fa-solid fa-box-open"
        var mappings = configuration.GetSection("WallSettings").GetSection("ToDoIconMappings").Get<List<ToDoIconMapping>>() ?? new List<ToDoIconMapping>();

        foreach (ToDoItemModel item in toDoItemModels)
        {
            var match = mappings.FirstOrDefault(m =>
                (m.Keywords ?? Array.Empty<string>()).Any(k =>
                    !string.IsNullOrEmpty(item.Title) &&
                    item.Title.Contains(k, StringComparison.InvariantCultureIgnoreCase)));

            item.Icon = match?.Icon ?? "fa-solid fa-box-open";
        }

        return new JsonResult(toDoItemModels);
    }

    public async Task<IActionResult> OnGetEventsAsync(string start, string end)
    {
        try
        {
            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);

            var events = await calendarService.GetEventsForDateRangeAsync(startDate, endDate);

            // Convert to FullCalendar format - use minute precision (no seconds)
            var fullCalendarEvents = events.Select(e => new
            {
                id = e.EventId,
                title = e.Title,
                start = e.IsAllDay ? e.StartDateTime.ToString("yyyy-MM-dd") : e.StartDateTime.ToString("yyyy-MM-ddTHH:mm"),
                end = e.IsAllDay ? e.EndDateTime.AddDays(1).ToString("yyyy-MM-dd") : e.EndDateTime.ToString("yyyy-MM-ddTHH:mm"),
                allDay = e.IsAllDay,
                description = e.Description,
                location = e.Location,
                backgroundColor = GetBackgroundColor(e.Color),
                borderColor = GetBorderColor(e.Color),
                textColor = "#000000"
            }).ToList();

            return new JsonResult(fullCalendarEvents);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading calendar events");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    private string GetBackgroundColor(string color)
    {
        return color switch
        {
            "blue" => "#e3f2fd",
            "green" => "#e8f5e9",
            "purple" => "#f3e5f5",
            "red" => "#ffebee",
            "orange" => "#fff3e0",
            "teal" => "#e0f2f1",
            "gray" => "#f5f5f5",
            _ => "#e3f2fd"
        };
    }

    private string GetBorderColor(string color)
    {
        return color switch
        {
            "blue" => "#2196f3",
            "green" => "#4caf50",
            "purple" => "#9c27b0",
            "red" => "#f44336",
            "orange" => "#ff9800",
            "teal" => "#009688",
            "gray" => "#9e9e9e",
            _ => "#2196f3"
        };
    }

    // Helper to round up a DateTimeOffset to the nearest interval
    private static DateTimeOffset RoundUpToNearest(DateTimeOffset dt, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            return dt;
        }

        long ticks = interval.Ticks;
        long remainder = dt.Ticks % ticks;
        return remainder == 0 ? dt : dt.AddTicks(ticks - remainder);
    }

    // Simple POCO for configuration binding
    private sealed class ToDoIconMapping
    {
        public string? Icon { get; set; }
        public string[]? Keywords { get; set; }
    }
}