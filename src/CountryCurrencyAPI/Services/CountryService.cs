using CountryCurrencyAPI.Data;
using CountryCurrencyAPI.DTOs;
using CountryCurrencyAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CountryCurrencyAPI.Services;

public class CountryService : ICountryService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<CountryService> _logger;
    private const string CountriesApiUrl = "https://restcountries.com/v2/all?fields=name,capital,region,population,flag,currencies";
    private const string ExchangeRateApiUrl = "https://open.er-api.com/v6/latest/USD";

    public CountryService(HttpClient httpClient, AppDbContext context, ILogger<CountryService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public async Task RefreshCountriesAsync()
    {
        try
        {
            _logger.LogInformation("Starting country refresh...");

            // Fetch countries data
            var countriesResponse = await _httpClient.GetAsync(CountriesApiUrl);
            if (!countriesResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Could not fetch data from RestCountries API. Status: {countriesResponse.StatusCode}");
            }

            var countriesJson = await countriesResponse.Content.ReadAsStringAsync();
            var countries = JsonSerializer.Deserialize<List<RestCountriesResponse>>(countriesJson);

            if (countries == null || !countries.Any())
            {
                throw new InvalidOperationException("No countries data received from API");
            }

            // Fetch exchange rates
            var exchangeRateResponse = await _httpClient.GetAsync(ExchangeRateApiUrl);
            if (!exchangeRateResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Could not fetch data from Exchange Rate API. Status: {exchangeRateResponse.StatusCode}");
            }

            var exchangeRateJson = await exchangeRateResponse.Content.ReadAsStringAsync();
            var exchangeRates = JsonSerializer.Deserialize<ExchangeRateResponse>(exchangeRateJson);

            if (exchangeRates?.Rates == null)
            {
                throw new InvalidOperationException("No exchange rates data received from API");
            }

            var refreshTime = DateTime.UtcNow;
            var processedCount = 0;

            foreach (var countryData in countries)
            {
                if (string.IsNullOrWhiteSpace(countryData.Name))
                    continue;

                // Handle currency extraction
                string? currencyCode = null;
                decimal? exchangeRate = null;
                decimal? estimatedGdp = null;

                if (countryData.Currencies != null && countryData.Currencies.Any())
                {
                    // Take the first currency code
                    currencyCode = countryData.Currencies.First().Code;

                    if (!string.IsNullOrWhiteSpace(currencyCode))
                    {
                        // Try to get exchange rate
                        if (exchangeRates.Rates.TryGetValue(currencyCode, out var rate))
                        {
                            exchangeRate = rate;

                            // Calculate estimated GDP
                            var random = new Random();
                            var multiplier = random.Next(1000, 2001); // 1000 to 2000 inclusive
                            estimatedGdp = (countryData.Population * multiplier) / exchangeRate;
                        }
                        else
                        {
                            // Currency code not found in exchange rates
                            exchangeRate = null;
                            estimatedGdp = null;
                        }
                    }
                }
                else
                {
                    // No currencies - set fields to null/0
                    currencyCode = null;
                    exchangeRate = null;
                    estimatedGdp = 0;
                }

                // Check if country exists (case-insensitive)
                var existingCountry = await _context.Countries
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == countryData.Name.ToLower());

                if (existingCountry != null)
                {
                    // Update existing country
                    existingCountry.Capital = countryData.Capital?.FirstOrDefault();
                    existingCountry.Region = countryData.Region;
                    existingCountry.Population = countryData.Population;
                    existingCountry.CurrencyCode = currencyCode;
                    existingCountry.ExchangeRate = exchangeRate;
                    existingCountry.EstimatedGdp = estimatedGdp;
                    existingCountry.FlagUrl = countryData.Flag;
                    existingCountry.LastRefreshedAt = refreshTime;
                }
                else
                {
                    // Insert new country
                    var newCountry = new Country
                    {
                        Name = countryData.Name,
                        Capital = countryData.Capital?.FirstOrDefault(),
                        Region = countryData.Region,
                        Population = countryData.Population,
                        CurrencyCode = currencyCode,
                        ExchangeRate = exchangeRate,
                        EstimatedGdp = estimatedGdp,
                        FlagUrl = countryData.Flag,
                        LastRefreshedAt = refreshTime
                    };
                    _context.Countries.Add(newCountry);
                }

                processedCount++;
            }

            await _context.SaveChangesAsync();

            // Update global last_refreshed_at timestamp
            var metadata = await _context.SystemMetadata
                .FirstOrDefaultAsync(m => m.KeyName == "last_refreshed_at");

            if (metadata != null)
            {
                metadata.KeyValue = refreshTime.ToString("O");
                metadata.UpdatedAt = refreshTime;
            }
            else
            {
                _context.SystemMetadata.Add(new SystemMetadata
                {
                    KeyName = "last_refreshed_at",
                    KeyValue = refreshTime.ToString("O"),
                    UpdatedAt = refreshTime
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully refreshed {processedCount} countries");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed during country refresh");
            throw new InvalidOperationException($"External data source unavailable: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during country refresh");
            throw;
        }
    }
}

