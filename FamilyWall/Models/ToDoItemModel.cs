namespace FamilyWall.Models;

public class ToDoItemModel(ToDoItem item)
{
    public string? Id { get; set; } = item.Id;

    public string? Importance { get; set; } = item.Importance;

    public bool? IsReminderOn { get; set; } = item.IsReminderOn;

    public string? Status { get; set; } = item.Status;

    public string? Title { get; set; } = item.Title;

    public DateTimeOffset? CreatedDateTime { get; set; } = item.CreatedDateTime;

    public DateTimeOffset? LastModifiedDateTime { get; set; } = item.LastModifiedDateTime;

    public DateTimeOffset? DueDate { get; set; } = item.DueDateTime?.DateTime;

    public List<string>? Categories { get; set; } = item.Categories;

    public string? Body { get; set; } = item.Body?.Content;

    public string? Icon { get; set; }
}