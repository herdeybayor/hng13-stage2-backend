using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CountryCurrencyAPI.Data;
using CountryCurrencyAPI.DTOs;
using CountryCurrencyAPI.Services;

namespace CountryCurrencyAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CountriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICountryService _countryService;
    private readonly IImageService _imageService;
    private readonly ILogger<CountriesController> _logger;

    public CountriesController(
        AppDbContext context,
        ICountryService countryService,
        IImageService imageService,
        ILogger<CountriesController> logger)
    {
        _context = context;
        _countryService = countryService;
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// Fetch all countries and exchange rates, then cache them in the database
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshCountries()
    {
        try
        {
            await _countryService.RefreshCountriesAsync();

            // Generate summary image after successful refresh
            var totalCountries = await _context.Countries.CountAsync();
            var topCountries = await _context.Countries
                .OrderByDescending(c => c.EstimatedGdp)
                .Take(5)
                .Select(c => new ValueTuple<string, decimal?>(c.Name, c.EstimatedGdp))
                .ToListAsync();

            var lastRefreshed = await GetLastRefreshedTimeAsync() ?? DateTime.UtcNow;

            await _imageService.GenerateSummaryImageAsync(totalCountries, topCountries, lastRefreshed);

            return Ok(new { message = "Countries refreshed successfully", total_countries = totalCountries });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("External data source"))
        {
            _logger.LogError(ex, "External API unavailable");
            return StatusCode(503, new
            {
                error = "External data source unavailable",
                details = ex.InnerException?.Message ?? ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing countries");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all countries from the database (supports filters and sorting)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCountries(
        [FromQuery] string? region,
        [FromQuery] string? currency,
        [FromQuery] string? sort)
    {
        try
        {
            var query = _context.Countries.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(region))
            {
                query = query.Where(c => c.Region != null && c.Region.ToLower() == region.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(currency))
            {
                query = query.Where(c => c.CurrencyCode != null && c.CurrencyCode.ToLower() == currency.ToLower());
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(sort))
            {
                query = sort.ToLower() switch
                {
                    "gdp_desc" => query.OrderByDescending(c => c.EstimatedGdp),
                    "gdp_asc" => query.OrderBy(c => c.EstimatedGdp),
                    "population_desc" => query.OrderByDescending(c => c.Population),
                    "population_asc" => query.OrderBy(c => c.Population),
                    "name_asc" => query.OrderBy(c => c.Name),
                    "name_desc" => query.OrderByDescending(c => c.Name),
                    _ => query.OrderBy(c => c.Name)
                };
            }
            else
            {
                query = query.OrderBy(c => c.Name);
            }

            var countries = await query.ToListAsync();

            var result = countries.Select(c => new CountryDto
            {
                Id = c.Id,
                Name = c.Name,
                Capital = c.Capital,
                Region = c.Region,
                Population = c.Population,
                CurrencyCode = c.CurrencyCode,
                ExchangeRate = c.ExchangeRate,
                EstimatedGdp = c.EstimatedGdp,
                FlagUrl = c.FlagUrl,
                LastRefreshedAt = c.LastRefreshedAt
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting countries");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get one country by name
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetCountryByName(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    error = "Validation failed",
                    details = new { name = "is required" }
                });
            }

            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

            if (country == null)
            {
                return NotFound(new { error = "Country not found" });
            }

            var result = new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                Capital = country.Capital,
                Region = country.Region,
                Population = country.Population,
                CurrencyCode = country.CurrencyCode,
                ExchangeRate = country.ExchangeRate,
                EstimatedGdp = country.EstimatedGdp,
                FlagUrl = country.FlagUrl,
                LastRefreshedAt = country.LastRefreshedAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting country by name");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a country record
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteCountry(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    error = "Validation failed",
                    details = new { name = "is required" }
                });
            }

            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

            if (country == null)
            {
                return NotFound(new { error = "Country not found" });
            }

            _context.Countries.Remove(country);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Country '{country.Name}' deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting country");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Show total countries and last refresh timestamp
    /// </summary>
    [HttpGet("/status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var totalCountries = await _context.Countries.CountAsync();
            var lastRefreshed = await GetLastRefreshedTimeAsync();

            return Ok(new
            {
                total_countries = totalCountries,
                last_refreshed_at = lastRefreshed?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Serve the generated summary image
    /// </summary>
    [HttpGet("image")]
    public IActionResult GetImage()
    {
        try
        {
            if (!_imageService.ImageExists())
            {
                return NotFound(new { error = "Summary image not found" });
            }

            var imagePath = _imageService.GetImagePath();
            var imageBytes = System.IO.File.ReadAllBytes(imagePath);
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving image");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task<DateTime?> GetLastRefreshedTimeAsync()
    {
        var metadata = await _context.SystemMetadata
            .FirstOrDefaultAsync(m => m.KeyName == "last_refreshed_at");

        if (metadata?.KeyValue != null && DateTime.TryParse(metadata.KeyValue, out var lastRefreshed))
        {
            return lastRefreshed;
        }

        return null;
    }
}