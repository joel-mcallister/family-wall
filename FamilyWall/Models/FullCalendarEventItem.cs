namespace FamilyWall.Models;

public class FullCalendarEventItem
{
    public string EventId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsAllDay { get; set; }
    public string Location { get; set; }
    public string Color { get; set; }
}