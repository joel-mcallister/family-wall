using System.Text.Json.Serialization;

namespace FamilyWall.Models;

public sealed class NationalWeatherServiceObservationValue
{
    [JsonPropertyName("unitCode")]
    public string? UnitCode { get; set; }

    [JsonPropertyName("value")]
    public double? Value { get; set; }
}