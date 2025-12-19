using FamilyWall.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace FamilyWall.Services;

public class GoogleCalendarService(IConfiguration configuration)
{
    private readonly string[] Scopes = { CalendarService.Scope.Calendar };

    private string GetCalendarId()
    {
        // Use configured calendar ID, or fall back to "primary" if not set
        return configuration["GoogleCalendar:CalendarId"] ?? "primary";
    }

    public async Task<List<FullCalendarEventItem>> GetEventsForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var service = await GetCalendarServiceAsync();
        var calendarId = GetCalendarId();

        var request = service.Events.List(calendarId);
        request.TimeMinDateTimeOffset = startDate;
        request.TimeMaxDateTimeOffset = endDate;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync();
        var calendarEvents = new List<FullCalendarEventItem>();

        foreach (var eventItem in events.Items)
        {
            DateTimeOffset? startDateTime = null;
            DateTimeOffset? endDateTime = null;
            bool isAllDay = false;

            if (eventItem.Start.DateTimeDateTimeOffset.HasValue)
            {
                startDateTime = eventItem.Start.DateTimeDateTimeOffset.Value;
                endDateTime = eventItem.End.DateTimeDateTimeOffset.Value;
            }
            else if (!string.IsNullOrEmpty(eventItem.Start.Date))
            {
                startDateTime = DateTime.Parse(eventItem.Start.Date);
                endDateTime = DateTime.Parse(eventItem.End.Date);
                isAllDay = true;
            }

            if (startDateTime.HasValue)
            {
                calendarEvents.Add(new FullCalendarEventItem
                {
                    EventId = eventItem.Id,
                    Title = eventItem.Summary ?? "Untitled Event",
                    Description = eventItem.Description,
                    StartDateTime = startDateTime.Value.LocalDateTime,
                    EndDateTime = endDateTime.Value.LocalDateTime,
                    IsAllDay = isAllDay,
                    Location = eventItem.Location,
                    Color = GetEventColor(eventItem.ColorId)
                });
            }
        }

        return calendarEvents;
    }

    public async Task<CalendarService> GetCalendarServiceAsync()
    {
        var credentialsPath = configuration["GoogleCalendar:CredentialsPath"];
        var tokenPath = configuration["GoogleCalendar:TokenPath"];
        var applicationName = configuration["GoogleCalendar:ApplicationName"];

        UserCredential credential;

        await using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                (await GoogleClientSecrets.FromStreamAsync(stream)).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenPath, true));
        }

        return new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });
    }

    public async Task<List<FullCalendarEventItem>> GetEventsForMonthAsync(int year, int month)
    {
        var service = await GetCalendarServiceAsync();
        var calendarId = GetCalendarId();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var request = service.Events.List(calendarId);
        request.TimeMinDateTimeOffset = startDate;
        request.TimeMaxDateTimeOffset = endDate;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync();
        var calendarEvents = new List<FullCalendarEventItem>();

        foreach (var eventItem in events.Items)
        {
            DateTimeOffset? startDateTime = null;
            bool isAllDay = false;

            if (eventItem.Start.DateTimeDateTimeOffset.HasValue)
            {
                startDateTime = eventItem.Start.DateTimeDateTimeOffset.Value;
            }
            else if (!string.IsNullOrEmpty(eventItem.Start.Date))
            {
                startDateTime = DateTime.Parse(eventItem.Start.Date);
                isAllDay = true;
            }

            if (startDateTime.HasValue)
            {
                calendarEvents.Add(new FullCalendarEventItem
                {
                    EventId = eventItem.Id,
                    Title = eventItem.Summary ?? "Untitled Event",
                    Description = eventItem.Description,
                    StartDateTime = startDateTime.Value.ToLocalTime().DateTime,
                    EndDateTime = eventItem.End.DateTimeDateTimeOffset?.ToLocalTime().DateTime ?? DateTime.Parse(eventItem.End.Date),
                    IsAllDay = isAllDay,
                    Location = eventItem.Location,
                    Color = GetEventColor(eventItem.ColorId)
                });
            }
        }

        return calendarEvents;
    }

    public async Task<string> CreateEventAsync(FullCalendarEventItem eventItem)
    {
        var service = await GetCalendarServiceAsync();
        var calendarId = GetCalendarId();

        var newEvent = new Event
        {
            Summary = eventItem.Title,
            Description = eventItem.Description,
            Location = eventItem.Location,
            Start = eventItem.IsAllDay
                ? new EventDateTime { Date = eventItem.StartDateTime.ToString("yyyy-MM-dd") }
                : new EventDateTime { DateTimeDateTimeOffset = eventItem.StartDateTime },
            End = eventItem.IsAllDay
                ? new EventDateTime { Date = eventItem.EndDateTime.ToString("yyyy-MM-dd") }
                : new EventDateTime { DateTimeDateTimeOffset = eventItem.EndDateTime }
        };

        var request = service.Events.Insert(newEvent, calendarId);
        var createdEvent = await request.ExecuteAsync();

        return createdEvent.Id;
    }

    public async Task UpdateEventAsync(FullCalendarEventItem eventItem)
    {
        var service = await GetCalendarServiceAsync();
        var calendarId = GetCalendarId();

        var eventToUpdate = await service.Events.Get(calendarId, eventItem.EventId).ExecuteAsync();

        eventToUpdate.Summary = eventItem.Title;
        eventToUpdate.Description = eventItem.Description;
        eventToUpdate.Location = eventItem.Location;
        eventToUpdate.Start = eventItem.IsAllDay
            ? new EventDateTime { Date = eventItem.StartDateTime.ToString("yyyy-MM-dd") }
            : new EventDateTime { DateTimeDateTimeOffset = eventItem.StartDateTime };
        eventToUpdate.End = eventItem.IsAllDay
            ? new EventDateTime { Date = eventItem.EndDateTime.ToString("yyyy-MM-dd") }
            : new EventDateTime { DateTimeDateTimeOffset = eventItem.EndDateTime };

        var request = service.Events.Update(eventToUpdate, calendarId, eventItem.EventId);
        await request.ExecuteAsync();
    }

    public async Task DeleteEventAsync(string eventId)
    {
        var service = await GetCalendarServiceAsync();
        var calendarId = GetCalendarId();

        var request = service.Events.Delete(calendarId, eventId);
        await request.ExecuteAsync();
    }

    private string GetEventColor(string colorId)
    {
        return colorId switch
        {
            "1" => "blue",
            "2" => "green",
            "3" => "purple",
            "4" => "red",
            "5" => "orange",
            "6" => "orange",
            "7" => "teal",
            "8" => "gray",
            "9" => "blue",
            "10" => "green",
            "11" => "red",
            _ => "blue"
        };
    }

    // Bonus: Method to list all available calendars
    public async Task<List<(string Id, string Name)>> GetAvailableCalendarsAsync()
    {
        var service = await GetCalendarServiceAsync();
        var request = service.CalendarList.List();
        var calendars = await request.ExecuteAsync();

        return calendars.Items
            .Select(c => (c.Id, c.Summary))
            .ToList();
    }
}