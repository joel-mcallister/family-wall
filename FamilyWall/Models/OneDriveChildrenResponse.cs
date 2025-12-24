using System.Text.Json.Serialization;

namespace FamilyWall.Models;

public sealed class OneDriveChildrenResponse
{
    public List<OneDriveItem> Value { get; set; } = new();
    [JsonPropertyName("@odata.nextLink")]
    public string? OdataNextLink { get; set; }
}