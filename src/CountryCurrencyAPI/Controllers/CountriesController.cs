using Microsoft.AspNetCore.Mvc;

namespace CountryCurrencyAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CountriesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCountries()
    {
        var countries = new[]
        {
            new { Name = "United States", Currency = "USD" },
            new { Name = "Canada", Currency = "CAD" },
            new { Name = "United Kingdom", Currency = "GBP" },
            new { Name = "Japan", Currency = "JPY" }
        };

        return Ok(countries);
    }
}