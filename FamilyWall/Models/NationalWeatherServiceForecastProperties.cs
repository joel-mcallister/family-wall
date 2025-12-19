using System.Text.Json.Serialization;

namespace FamilyWall.Models;

public sealed class NationalWeatherServiceForecastProperties
{
    [JsonPropertyName("updated")]
    public DateTimeOffset? Updated { get; set; }

    [JsonPropertyName("periods")]
    public List<NationalWeatherServiceForecastPeriod> Periods { get; set; } = new();
}