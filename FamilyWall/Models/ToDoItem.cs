namespace FamilyWall.Models;

public sealed class ToDoItem
{
    public string? Id { get; set; }

    public string? Importance { get; set; }

    public bool? IsReminderOn { get; set; }

    public string? Status { get; set; }

    public string? Title { get; set; }

    public DateTimeOffset? CreatedDateTime { get; set; }

    public DateTimeOffset? LastModifiedDateTime { get; set; }

    public ToDoDueDate? DueDateTime { get; set; }

    public List<string>? Categories { get; set; }

    public ToDoBody? Body { get; set; }
}