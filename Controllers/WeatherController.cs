using Microsoft.AspNetCore.Mvc;
using SprintTracker.Api.Models.DTOs;
using SprintTracker.Api.Services;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    [HttpGet("geocode")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationSuggestionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<LocationSuggestionDto>>>> Geocode([FromQuery] string place)
    {
        if (string.IsNullOrWhiteSpace(place))
            return BadRequest(new ApiResponse<List<LocationSuggestionDto>>(false, null, "Place is required", null));

        try
        {
            var results = await _weatherService.GeocodeAsync(place);
            return Ok(new ApiResponse<List<LocationSuggestionDto>>(true, results, null, null));
        }
        catch (InvalidOperationException ex)
        {
            // Configuration issue (missing API key)
            _logger.LogWarning(ex, "Configuration issue during geocode for {Place}", place);
            return BadRequest(new ApiResponse<List<LocationSuggestionDto>>(false, null, ex.Message, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during geocode for {Place}", place);
            return StatusCode(500, new ApiResponse<List<LocationSuggestionDto>>(false, null, "Failed to fetch location suggestions", null));
        }
    }

    [HttpGet("report")]
    [ProducesResponseType(typeof(ApiResponse<WeatherReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WeatherReportDto>>> GetReport([FromQuery] double lat, [FromQuery] double lon, [FromQuery] string units = "metric")
    {
        try
        {
            var report = await _weatherService.GetWeatherByCoordsAsync(lat, lon, units);
            return Ok(new ApiResponse<WeatherReportDto>(true, report, null, null));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Configuration issue fetching weather");
            return BadRequest(new ApiResponse<WeatherReportDto>(false, null, ex.Message, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather report for {Lat},{Lon}", lat, lon);
            return StatusCode(500, new ApiResponse<WeatherReportDto>(false, null, "Failed to fetch weather report", null));
        }
    }
}
