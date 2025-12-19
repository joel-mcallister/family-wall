namespace FamilyWall.Models;

public sealed class OneDriveChildrenResponse
{
    public List<OneDriveItem> Value { get; set; } = new();
    public string? OdataNextLink { get; set; }
}