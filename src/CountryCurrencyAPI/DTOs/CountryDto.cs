using System.Text.Json.Serialization;

namespace CountryCurrencyAPI.DTOs;

public class CountryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("capital")]
    public string? Capital { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("population")]
    public long Population { get; set; }

    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("exchange_rate")]
    public decimal? ExchangeRate { get; set; }

    [JsonPropertyName("estimated_gdp")]
    public decimal? EstimatedGdp { get; set; }

    [JsonPropertyName("flag_url")]
    public string? FlagUrl { get; set; }

    [JsonPropertyName("last_refreshed_at")]
    public DateTime LastRefreshedAt { get; set; }
}

