using System.Text.Json.Serialization;

namespace FamilyWall.Models;

public sealed class NationalWeatherServiceForecastPeriod
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("isDaytime")]
    public bool IsDaytime { get; set; }

    [JsonPropertyName("temperature")]
    public int Temperature { get; set; }

    [JsonPropertyName("temperatureUnit")]
    public string? TemperatureUnit { get; set; }

    [JsonPropertyName("windSpeed")]
    public string? WindSpeed { get; set; }

    [JsonPropertyName("windDirection")]
    public string? WindDirection { get; set; }

    [JsonPropertyName("shortForecast")]
    public string? ShortForecast { get; set; }

    [JsonPropertyName("detailedForecast")]
    public string? DetailedForecast { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}