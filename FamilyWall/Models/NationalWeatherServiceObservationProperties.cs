using System.Text.Json.Serialization;

namespace FamilyWall.Models;

public sealed class NationalWeatherServiceObservationProperties
{
    [JsonPropertyName("stationName")]
    public string? StationName { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? LastUpdated { get; set; }

    [JsonPropertyName("temperature")]
    public NationalWeatherServiceObservationValue? Temperature { get; set; }

    [JsonPropertyName("dewpoint")]
    public NationalWeatherServiceObservationValue? DewPoint { get; set; }

    [JsonPropertyName("windDirection")]
    public NationalWeatherServiceObservationValue? WindDirection { get; set; }

    [JsonPropertyName("windSpeed")]
    public NationalWeatherServiceObservationValue? WindSpeed { get; set; }

    [JsonPropertyName("relativeHumidity")]
    public NationalWeatherServiceObservationValue? Humidity { get; set; }

    [JsonPropertyName("heatIndex")]
    public NationalWeatherServiceObservationValue? HeatIndex { get; set; }

    [JsonPropertyName("windChill")]
    public NationalWeatherServiceObservationValue? WindChill { get; set; }
}