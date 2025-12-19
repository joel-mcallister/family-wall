using System.Text.Json.Serialization;

namespace FamilyWall.Models;

public sealed class NationalWeatherServiceForecastResponse
{
    [JsonPropertyName("properties")]
    public NationalWeatherServiceForecastProperties? Properties { get; set; }

    [JsonPropertyName("observation")]
    public NationalWeatherServiceObservation? Observation { get; set; }
}