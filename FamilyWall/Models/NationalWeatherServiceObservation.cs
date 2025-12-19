using System.Text.Json.Serialization;

namespace FamilyWall.Models;

public sealed class NationalWeatherServiceObservation
{
    [JsonPropertyName("properties")]
    public NationalWeatherServiceObservationProperties? Properties { get; set; }
}