using FamilyWall.Models;
using System.Text.Json;

namespace FamilyWall.Services;

public sealed class NwsWeatherClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Gets the weather at a given gridpoint from the National Weather Service API.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Data</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<NationalWeatherServiceForecastResponse?> GetGridpointForecastAsync(
        CancellationToken ct = default)
    {
        using var http = httpClientFactory.CreateClient("nws");

        var office = configuration["NationalWeatherService:Office"] ?? "TAE";
        var gridX = configuration["NationalWeatherService:GridX"] ?? "86";
        var gridY = configuration["NationalWeatherService:GridY"] ?? "91";
        var url = $"gridpoints/{office}/{gridX},{gridY}/forecast";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        // Helpful error body for troubleshooting (rate limits, etc.)
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"NWS request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var nwsForecastResponse = await JsonSerializer.DeserializeAsync<NationalWeatherServiceForecastResponse>(stream, _jsonOptions, ct) ?? new NationalWeatherServiceForecastResponse();

        if (nwsForecastResponse.Properties != null)
        {
            int maxDays = int.Parse(configuration["NationalWeatherService:MaxForecastDays"] ?? "3");
            nwsForecastResponse.Properties.Periods = nwsForecastResponse.Properties.Periods
                .OrderBy(x => x.StartTime).Take(maxDays).ToList();
        }

        return nwsForecastResponse;
    }

    public async Task<NationalWeatherServiceObservation?> GetStationObservationAsync(
        CancellationToken ct = default)
    {
        using var http = httpClientFactory.CreateClient("nws");
        var stationId = configuration["NationalWeatherService:WeatherStationId"] ?? "1296W";
        var url = $"stations/{stationId}/observations/latest";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        // Helpful error body for troubleshooting (rate limits, etc.)
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"NWS request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<NationalWeatherServiceObservation>(stream, _jsonOptions, ct);

        if (result?.Properties != null)
        {
            // Convert Celsius -> Fahrenheit using floating point math.
            if (result.Properties.Temperature?.Value is double temp)
            {
                result.Properties.Temperature.Value = temp * 9.0 / 5.0 + 32.0;
            }

            if (result.Properties.HeatIndex?.Value is double heat)
            {
                result.Properties.HeatIndex.Value = heat * 9.0 / 5.0 + 32.0;
            }

            if (result.Properties.WindChill?.Value is double windChill)
            {
                result.Properties.WindChill.Value = windChill * 9.0 / 5.0 + 32.0;
            }

            if (result.Properties.DewPoint?.Value is double dew)
            {
                result.Properties.DewPoint.Value = dew * 9.0 / 5.0 + 32.0;
            }

            // Convert wind speed from km/h to mph if present.
            if (result.Properties.WindSpeed?.Value is double windSpeed)
            {
                result.Properties.WindSpeed.Value = windSpeed / 1.609344;
            }

            // Convert humidity to fraction (if original is percent).
            if (result.Properties.Humidity?.Value is double humidity)
            {
                result.Properties.Humidity.Value = humidity / 100.0;
            }
        }

        return result;
    }
}