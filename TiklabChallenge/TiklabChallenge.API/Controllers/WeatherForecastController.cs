using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;

namespace TiklabChallenge.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = AppRoles.Student)]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        [HttpGet("db")]
        public async Task<IActionResult> GetWeatherForecastDB()
        {
            var weatherForecasts = await _unitOfWork.WeatherForecasts.GetAllAsync();
            if (weatherForecasts == null || !weatherForecasts.Any())
            {
                return NotFound("No weather forecasts found.");
            }
            return Ok(weatherForecasts);
        }

        [HttpPost]
        public async Task<IActionResult> AddWeatherForecast([FromBody] WeatherForecast weatherForecast)
        {
            if (weatherForecast == null)
            {
                return BadRequest("Weather forecast cannot be null.");
            }
            await _unitOfWork.WeatherForecasts.AddAsync(weatherForecast);
            await _unitOfWork.CommitAsync();
            return Ok();
        }
    }
}
