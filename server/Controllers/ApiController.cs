using Microsoft.AspNetCore.Mvc;
using Shared.Meta.Api;

namespace server.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController(IMeta meta, ILogger<ApiController> logger) 
    : ControllerBase, IMeta
{
    private static readonly string[] Summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        Shared.StaticLog.Info("==== api request ====");
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
    
    [Route("datetime")]
    public ValueTask<string> GetDateTime(CancellationToken cancellationToken) 
        => meta.GetDateTime(cancellationToken);
}