namespace FamilyWall.Models;

public sealed class ToDoResponse
{
    public List<ToDoItem> Value { get; set; } = new();

    public string? OdataNextLink { get; set; }
}