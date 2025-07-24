using Microsoft.AspNetCore.Mvc;

namespace AgenticOrderingSystem.API.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets weather forecast for the next 5 days
    /// </summary>
    /// <returns>Collection of weather forecasts</returns>
    [HttpGet(Name = "GetWeatherForecast")]
    [ProducesResponseType(typeof(IEnumerable<WeatherForecast>), 200)]
    public IActionResult GetWeatherForecast()
    {
        _logger.LogInformation("Getting weather forecast for the next 5 days");
        
        var forecasts = GetWeatherData();
        return Ok(forecasts);
    }

    /// <summary>
    /// Gets weather forecast for a specific city
    /// </summary>
    /// <param name="city">City name</param>
    /// <returns>Weather forecast for the specified city</returns>
    [HttpGet("{city}")]
    [ProducesResponseType(typeof(WeatherForecast), 200)]
    [ProducesResponseType(400)]
    public IActionResult GetWeatherByCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            _logger.LogWarning("Invalid city name provided: {City}", city);
            return BadRequest("City name cannot be empty");
        }

        _logger.LogInformation("Getting weather forecast for city: {City}", city);
        
        var forecast = new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            City = city
        };

        return Ok(forecast);
    }

    /// <summary>
    /// Gets weather forecast with temperature in Fahrenheit
    /// </summary>
    /// <returns>Collection of weather forecasts with Fahrenheit temperatures</returns>
    [HttpGet("fahrenheit")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public IActionResult GetWeatherForecastInFahrenheit()
    {
        _logger.LogInformation("Getting weather forecast in Fahrenheit");
        
        var forecasts = GetWeatherData().Select(f => new
        {
            Date = f.Date,
            TemperatureF = f.TemperatureF,
            TemperatureC = f.TemperatureC,
            Summary = f.Summary,
            City = f.City
        });

        return Ok(forecasts);
    }

    private IEnumerable<WeatherForecast> GetWeatherData()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
