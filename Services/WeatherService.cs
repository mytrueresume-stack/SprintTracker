using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SprintTracker.Api.Models.DTOs;

namespace SprintTracker.Api.Services;

public interface IWeatherService
{
    Task<List<LocationSuggestionDto>> GeocodeAsync(string place, int limit = 5);
    Task<WeatherReportDto> GetWeatherByCoordsAsync(double lat, double lon, string units = "metric");
}

public class WeatherService : IWeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherService> _logger;

    private const string GeocodeUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

    public WeatherService(IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<LocationSuggestionDto>> GeocodeAsync(string place, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(place)) return new List<LocationSuggestionDto>();
        var client = _httpClientFactory.CreateClient();
        var q = System.Web.HttpUtility.UrlEncode(place);
        var url = $"{GeocodeUrl}?name={q}&count={limit}";
        var res = await client.GetAsync(url);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Geocode request failed with status {Status}", res.StatusCode);
            throw new Exception("Failed to fetch location suggestions");
        }
        var raw = await res.Content.ReadFromJsonAsync<GeocodeRaw>();
        var list = (raw?.results ?? new List<GeocodeResult>()).Select(d => new LocationSuggestionDto($"{d.name}{(string.IsNullOrEmpty(d.admin1) ? "" : ", " + d.admin1)}{(string.IsNullOrEmpty(d.country) ? "" : ", " + d.country)}", d.latitude, d.longitude)).ToList();
        return list;
    }

    public async Task<WeatherReportDto> GetWeatherByCoordsAsync(double lat, double lon, string units = "metric")
    {
        var client = _httpClientFactory.CreateClient();
        // Request current + hourly (including humidity, uvi, pressure), and daily summaries
        var hourlyParams = "temperature_2m,relativehumidity_2m,apparent_temperature,uv_index,precipitation_probability,windspeed_10m,winddirection_10m,surface_pressure,weathercode";
        var dailyParams = "temperature_2m_max,temperature_2m_min,precipitation_probability_max,sunrise,sunset,weathercode";
        var url = $"{ForecastUrl}?latitude={lat}&longitude={lon}&current_weather=true&hourly={hourlyParams}&daily={dailyParams}&timezone=auto";
        _logger.LogDebug("Fetching Open-Meteo forecast: {Url}", url);
        var res = await client.GetAsync(url);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Forecast request failed with status {Status}", res.StatusCode);
            throw new Exception("Failed to fetch weather data");
        }
        var raw = await res.Content.ReadFromJsonAsync<ForecastRaw>();
        if (raw == null) throw new Exception("Invalid weather response");

        // Build current by combining current_weather and matching hourly indices
        var currentDtMs = raw.current_weather.time != null ? DateTime.Parse(raw.current_weather.time).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds : raw.current_weather.time == null ? DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds : raw.current_weather.time != null ? DateTime.Parse(raw.current_weather.time).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds : 0;

        // Find the index in hourly times that matches current time
        int idx = -1;
        if (raw.hourly?.time != null && raw.hourly.time.Count > 0)
        {
            idx = raw.hourly.time.FindIndex(t => t == raw.current_weather.time);
            if (idx < 0) idx = 0;
        }

        int weatherCode = raw.current_weather.weathercode;
        string desc = WeatherCodeToDescription(weatherCode);
        string icon = WeatherCodeToIcon(weatherCode);

        int? pressure = null; double? uvi = null; int? humidity = null; double? feelsLike = null; int? windDeg = null; double? windSpeed = null;
        if (idx >= 0 && raw.hourly != null)
        {
            if (raw.hourly.surface_pressure != null && raw.hourly.surface_pressure.Count > idx) pressure = (int)Math.Round(raw.hourly.surface_pressure[idx]);
            if (raw.hourly.uv_index != null && raw.hourly.uv_index.Count > idx) uvi = raw.hourly.uv_index[idx];
            if (raw.hourly.relativehumidity_2m != null && raw.hourly.relativehumidity_2m.Count > idx) humidity = (int)Math.Round(raw.hourly.relativehumidity_2m[idx]);
            if (raw.hourly.apparent_temperature != null && raw.hourly.apparent_temperature.Count > idx) feelsLike = Math.Round(raw.hourly.apparent_temperature[idx], 1);
            if (raw.hourly.winddirection_10m != null && raw.hourly.winddirection_10m.Count > idx) windDeg = (int)Math.Round(raw.hourly.winddirection_10m[idx]);
            if (raw.hourly.windspeed_10m != null && raw.hourly.windspeed_10m.Count > idx) windSpeed = Math.Round(raw.hourly.windspeed_10m[idx], 1);
        }

        // Build current DTO
        var current = new CurrentWeatherDto(
            (long)(DateTime.Parse(raw.current_weather.time).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds),
            raw.current_weather.temperature,
            feelsLike,
            humidity,
            windSpeed,
            windDeg,
            pressure,
            uvi,
            raw.current_weather.weathercode,
            desc,
            icon,
            raw.daily?.sunrise?.FirstOrDefault() != null ? (long)(DateTime.Parse(raw.daily.sunrise.First()).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds) : (long?)null,
            raw.daily?.sunset?.FirstOrDefault() != null ? (long)(DateTime.Parse(raw.daily.sunset.First()).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds) : (long?)null
        );

        var hourly = new List<HourlyWeatherDto>();
        if (raw.hourly != null && raw.hourly.time != null)
        {
            for (int i = 0; i < raw.hourly.time.Count && i < 48; i++)
            {
                int? hc = raw.hourly.weathercode != null && raw.hourly.weathercode.Count > i ? raw.hourly.weathercode[i] : (int?)null;
                double? hpop = raw.hourly.precipitation_probability != null && raw.hourly.precipitation_probability.Count > i ? raw.hourly.precipitation_probability[i] : (double?)null;
                double? huvi = raw.hourly.uv_index != null && raw.hourly.uv_index.Count > i ? raw.hourly.uv_index[i] : (double?)null;
                int? hpressure = raw.hourly.surface_pressure != null && raw.hourly.surface_pressure.Count > i ? (int?)Math.Round(raw.hourly.surface_pressure[i]) : null;
                int? hwinddeg = raw.hourly.winddirection_10m != null && raw.hourly.winddirection_10m.Count > i ? (int?)Math.Round(raw.hourly.winddirection_10m[i]) : null;
                double? hwind = raw.hourly.windspeed_10m != null && raw.hourly.windspeed_10m.Count > i ? raw.hourly.windspeed_10m[i] : (double?)null;
                hourly.Add(new HourlyWeatherDto(
                    (long)(DateTime.Parse(raw.hourly.time[i]).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds),
                    raw.hourly.temperature_2m[i],
                    hpop,
                    hwind,
                    hwinddeg,
                    huvi,
                    hpressure,
                    hc
                ));
            }
        }

        var daily = new List<DailyWeatherDto>();
        if (raw.daily != null && raw.daily.time != null)
        {
            for (int i = 0; i < raw.daily.time.Count && i < raw.daily.time.Count; i++)
            {
                int wc = raw.daily.weathercode != null && raw.daily.weathercode.Count > i ? raw.daily.weathercode[i] : 0;
                daily.Add(new DailyWeatherDto(
                    (long)(DateTime.Parse(raw.daily.time[i]).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds),
                    raw.daily.temperature_2m_min[i],
                    raw.daily.temperature_2m_max[i],
                    raw.daily.precipitation_probability_max != null && raw.daily.precipitation_probability_max.Count > i ? raw.daily.precipitation_probability_max[i] : (double?)null,
                    (long)(DateTime.Parse(raw.daily.sunrise[i]).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds),
                    (long)(DateTime.Parse(raw.daily.sunset[i]).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds),
                    wc
                ));
            }
        }

        return new WeatherReportDto(raw.timezone, raw.timezone_offset, current, hourly, daily);
    }

    // Weather code mapping to description/icon
    private static string WeatherCodeToDescription(int code)
    {
        return code switch
        {
            0 => "Clear sky",
            1 or 2 => "Mainly clear / partly cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rain",
            71 or 73 or 75 => "Snow",
            80 or 81 or 82 => "Rain showers",
            95 or 96 or 99 => "Thunderstorm",
            _ => "Unknown"
        };
    }

    private static string WeatherCodeToIcon(int code)
    {
        return code switch
        {
            0 => "☀️",
            1 or 2 => "⛅",
            3 => "☁️",
            45 or 48 => "🌫️",
            51 or 53 or 55 => "🌦️",
            61 or 63 or 65 => "🌧️",
            71 or 73 or 75 => "❄️",
            80 or 81 or 82 => "🌧️",
            95 or 96 or 99 => "⛈️",
            _ => "🌈"
        };
    }

    // Raw classes
    private class GeocodeRaw { public List<GeocodeResult>? results { get; set; } }
    private class GeocodeResult { public string name { get; set; } = ""; public string? admin1 { get; set; } public string? country { get; set; } public double latitude { get; set; } public double longitude { get; set; } }

    private class ForecastRaw
    {
        public string timezone { get; set; } = "";
        public int timezone_offset { get; set; }
        public CurrentWeatherRaw current_weather { get; set; } = new CurrentWeatherRaw();
        public HourlyRaw? hourly { get; set; }
        public DailyRaw? daily { get; set; }
    }

    private class CurrentWeatherRaw { public string time { get; set; } = ""; public double temperature { get; set; } public double windspeed { get; set; } public int winddirection { get; set; } public int weathercode { get; set; } }

    private class HourlyRaw
    {
        public List<string> time { get; set; } = new();
        public List<double> temperature_2m { get; set; } = new();
        public List<double>? relativehumidity_2m { get; set; }
        public List<double>? apparent_temperature { get; set; }
        public List<double>? uv_index { get; set; }
        public List<double>? precipitation_probability { get; set; }
        // Open-Meteo names
        public List<double>? windspeed_10m { get; set; }
        public List<double>? winddirection_10m { get; set; }
        public List<double>? surface_pressure { get; set; }
        public List<int>? weathercode { get; set; }
    }

    private class DailyRaw
    {
        public List<string> time { get; set; } = new();
        public List<double> temperature_2m_min { get; set; } = new();
        public List<double> temperature_2m_max { get; set; } = new();
        public List<double>? precipitation_probability_max { get; set; }
        public List<string> sunrise { get; set; } = new();
        public List<string> sunset { get; set; } = new();
        public List<int>? weathercode { get; set; }
    }
}
