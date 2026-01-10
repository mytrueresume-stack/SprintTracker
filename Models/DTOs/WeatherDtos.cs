namespace SprintTracker.Api.Models.DTOs;

public record LocationSuggestionDto(string Name, double Lat, double Lon);

// Simplified weather DTOs for Open-Meteo mapping
public record CurrentWeatherDto(
    long Dt,
    double Temp,
    double? FeelsLike,
    int? Humidity,
    double? WindSpeed,
    int? WindDeg,
    int? Pressure,
    double? Uvi,
    int WeatherCode,
    string WeatherDescription,
    string WeatherIcon,
    long? Sunrise,
    long? Sunset
);

public record HourlyWeatherDto(long Dt, double Temp, double? Pop, double? WindSpeed, int? WindDeg, double? Uvi, int? Pressure, int? WeatherCode);

public record DailyWeatherDto(long Dt, double TempMin, double TempMax, double? Pop, long Sunrise, long Sunset, int WeatherCode);

public record WeatherReportDto(string Timezone, int TimezoneOffset, CurrentWeatherDto Current, List<HourlyWeatherDto> Hourly, List<DailyWeatherDto> Daily);
